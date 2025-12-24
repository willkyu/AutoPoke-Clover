using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Sentis;
using UnityEngine;

public class Detector : IDisposable
{
    private readonly int maxSize;
    private readonly List<DetectCore> detectCores;
    private readonly ConcurrentQueue<int> available;
    private readonly object lockObj = new object();

    public ModelAsset modelAsset;
    private readonly Dictionary<DetectionClass, float> classThresholds;
    private List<DetectionClass> classNames;
    private float blackThresholds = 0.8f;
    private int activeCount;

    // —— 内置 PumpHub：确保主线程每帧 Pump —— //
    private static DetectorPumpHub hub;

    private static void EnsureHub()
    {
        if (hub != null) return;
        var go = new GameObject("__DetectorPumpHub");
        UnityEngine.Object.DontDestroyOnLoad(go);
        hub = go.AddComponent<DetectorPumpHub>();
    }

    public Detector(int maxSize, List<DetectCore> detectCores, ModelAsset modelAsset, Dictionary<DetectionClass, float> classThresholds)
    {
        if (detectCores == null || detectCores.Count == 0)
            throw new ArgumentException("detectors list must not be empty");

        this.maxSize = maxSize;
        this.detectCores = detectCores;
        this.activeCount = detectCores.Count;

        this.modelAsset = modelAsset;
        this.classThresholds = classThresholds;
        this.classNames = classThresholds.Keys.ToList();

        this.available = new ConcurrentQueue<int>();
        for (int i = 0; i < detectCores.Count; i++)
            available.Enqueue(i);

        EnsureHub();
        hub.Register(this);
    }

    public static Detector Init(int initSize, int maxSize, ModelAsset modelAsset, Dictionary<DetectionClass, float> classThresholds)
    {
        var classes = classThresholds.Keys.ToList();
        var cores = new List<DetectCore>();
        for (int i = 0; i < initSize; i++)
            cores.Add(new DetectCore(modelAsset, classes, useGPU: true));
        var pool = new Detector(maxSize, cores, modelAsset, classThresholds);
        return pool;
    }

    // —— 外部仍同步调用：返回类别分组（阈值过滤） —— //
    public List<DetectionClass> Detect(byte[] image, bool detectBlack = false, int timeoutMillis = 1000)
    {
        var scores = RawDetect(image, detectBlack, timeoutMillis);
        var list = new List<DetectionClass>();
        foreach (var kv in classThresholds)
            if (scores.TryGetValue(kv.Key, out var s) && s > kv.Value)
                list.Add(kv.Key);
        // Debug.Log(scores.ToString());
        Debug.Log(string.Join(
    ", ",
    scores.Select(kv => $"{kv.Key}={kv.Value:F3}")
));
        return list;
    }

    // —— 外部同步调用，返回原始分数 —— //
    public Dictionary<DetectionClass, float> RawDetect(byte[] image, bool detectBlack = false, int timeoutMillis = 1000)
    {
        if (!TryGetAvailableDetector(timeoutMillis, out int detectorId))
        {
            AddCore();
            if (!TryGetAvailableDetector(timeoutMillis, out detectorId))
                throw new TimeoutException("Timeout waiting for available detector");
        }

        var core = detectCores[detectorId];
        if (core == null)
        {
            available.Enqueue(detectorId);
            throw new ObjectDisposedException("DetectCore");
        }

        try
        {
            // 主线程：走同步快路径（立即执行 GPU，避免等待下一帧）
            if (MainThreadDispatcher.IsMainThread)
            {
                // Debug.Log("[Detector] main thread.");
                var res = core.DetectSyncOnMainThread(image, detectBlack);
                return res;
            }
            else
            {
                // Debug.Log("[Detector] side thread.");
                // 后台线程：走异步路线C（后台预/后处理 + 主线程轻量GPU + PumpHub驱动）
                var task = core.DetectAsync(image, detectBlack);

                // 完成时归还 id（不阻塞主线程）
                task.ContinueWith(_ => available.Enqueue(detectorId), TaskScheduler.Default);

                return task.GetAwaiter().GetResult(); // 同步等待当前后台线程（主线程不受阻）
            }
        }
        catch
        {
            available.Enqueue(detectorId);
            throw;
        }
        finally
        {
            // 如果是主线程快路径，需要在此处归还
            if (MainThreadDispatcher.IsMainThread)
                available.Enqueue(detectorId);
        }
    }

    // —— 仅CPU：黑图判定（可保持同步） —— //
    public bool DetectBlack(byte[] image, float? blackThresholds = null, int timeoutMillis = 1000)
    {
        if (!TryGetAvailableDetector(timeoutMillis, out int detectorId))
        {
            AddCore();
            if (!TryGetAvailableDetector(timeoutMillis, out detectorId))
                throw new TimeoutException("Timeout waiting for available detector");
        }

        try
        {
            var ratio = detectCores[detectorId].ComputeBlackPercent(image);
            return ratio > (blackThresholds ?? this.blackThresholds);
        }
        finally
        {
            available.Enqueue(detectorId);
        }
    }

    private bool TryGetAvailableDetector(int timeoutMillis, out int id)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMillis)
        {
            if (available.TryDequeue(out id))
                return true;
            Thread.Sleep(1);
        }
        id = -1;
        return false;
    }

    private void AddCore()
    {
        lock (lockObj)
        {
            if (detectCores.Count >= maxSize)
                return; // 或抛出：达上限

            var newDetector = new DetectCore(modelAsset, classNames, useGPU: true);
            detectCores.Add(newDetector);
            activeCount++;
            available.Enqueue(detectCores.Count - 1);
        }
    }

    public bool RemoveCore()
    {
        lock (lockObj)
        {
            if (available.TryDequeue(out int id))
            {
                if (id >= 0 && id < detectCores.Count && detectCores[id] != null)
                {
                    detectCores[id].Dispose();
                    detectCores[id] = null;
                    activeCount--;
                    return true;
                }
            }
            return false;
        }
    }

    public int PoolSize => activeCount;
    public int BusyCount => PoolSize - available.Count;

    // —— 被 PumpHub 调用：在主线程每帧 pump 全部 core —— //
    internal void PumpAllCoresOnce(int maxPerCore = 2)
    {
        foreach (var c in detectCores)
            c?.PumpOnce(maxPerCore);
    }

    public void Dispose()
    {
        hub?.Unregister(this);
        foreach (var c in detectCores) c?.Dispose();
    }

    // —— 内置 PumpHub（主线程 MonoBehaviour） —— //
    private class DetectorPumpHub : MonoBehaviour
    {
        private readonly HashSet<Detector> detectors = new HashSet<Detector>();
        public void Register(Detector d) => detectors.Add(d);
        public void Unregister(Detector d) => detectors.Remove(d);

        private void Update()
        {
            foreach (var d in detectors)
                d.PumpAllCoresOnce(maxPerCore: 2); // 可按帧预算调整
        }
    }
}
