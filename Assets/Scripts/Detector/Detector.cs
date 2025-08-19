using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.Sentis;
using UnityEngine;

public class Detector
{
    private readonly int maxSize;
    private readonly List<DetectCore> detectCores;
    private readonly ConcurrentQueue<int> available;
    private readonly object lockObj = new object();
    public ModelAsset modelAsset;
    public List<DetectionClass> classNames;
    private Dictionary<DetectionClass, float> classThresholds;
    private float blackThresholds = 0.8f;
    private int activeCount;

    // private static UnityEngine.UI.Text fpsText;


    public Detector(int maxSize, List<DetectCore> detectCores, ModelAsset modelAsset, Dictionary<DetectionClass, float> classThresholds)
    {
        if (detectCores == null || detectCores.Count == 0)
            throw new ArgumentException("detectors list must not be empty");

        this.maxSize = maxSize;
        this.detectCores = detectCores;
        this.activeCount = detectCores.Count;

        this.modelAsset = modelAsset;
        this.classThresholds = classThresholds;
        // this.classNames = classNames;
        this.available = new ConcurrentQueue<int>();

        for (int i = 0; i < detectCores.Count; i++)
            available.Enqueue(i);

        // MainThreadDispatcher.Enqueue(() =>
        // {
        //     if (fpsText == null)
        //         fpsText = GameObject.Find("FPS")?.GetComponent<UnityEngine.UI.Text>();
        // });
    }

    public static Detector Init(int initSize, int maxSize, ModelAsset modelAsset, Dictionary<DetectionClass, float> classThresholds)
    {
        var detectors = new List<DetectCore>();
        for (int i = 0; i < initSize; i++)
        {
            detectors.Add(new DetectCore(modelAsset, classThresholds.Keys.ToList()));
        }
        return new Detector(maxSize, detectors, modelAsset, classThresholds);
    }

    public Dictionary<DetectionClass, float> RawDetect(byte[] image, bool detectBlack = false, int timeoutMillis = 1000)
    {
        if (!TryGetAvailableDetector(timeoutMillis, out int detectorId))
        {
            AddCore();
            if (!TryGetAvailableDetector(timeoutMillis, out detectorId))
                throw new TimeoutException("Timeout waiting for available detector");
        }

        try
        {
            Dictionary<DetectionClass, float> res = MainThreadDispatcher.InvokeSync(() =>
            {
                return detectCores[detectorId].Run(image, detectBlack);
            });
            return res;
        }
        finally
        {
            available.Enqueue(detectorId); // 释放
        }
    }

    public List<DetectionClass> Detect(byte[] image, bool detectBlack = false, int timeoutMillis = 1000)
    {
        // var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        if (!TryGetAvailableDetector(timeoutMillis, out int detectorId))
        {
            AddCore();
            if (!TryGetAvailableDetector(timeoutMillis, out detectorId))
                throw new TimeoutException("Timeout waiting for available detector");
        }

        try
        {
            // Dictionary<string, float> res = detectCores[detectorId].Run(image, detectBlack);
            Dictionary<DetectionClass, float> res = MainThreadDispatcher.InvokeSync(() =>
            {
                return detectCores[detectorId].Run(image, detectBlack);
            });
            // stopwatch.Stop();
            // float fps = 1000f / stopwatch.ElapsedMilliseconds; // 毫秒转FPS
            // Debug.Log($"Detector FPS: {fps:F2}");
            // MainThreadDispatcher.Enqueue(() =>
            // {
            //     fpsText.text = $"FPS: {fps:F2}";
            // });
            return classThresholds.Keys.Where(key => classThresholds[key] < res[key]).ToList();
        }
        finally
        {
            available.Enqueue(detectorId); // 释放
        }
    }

    public bool DetectBlack(byte[] image, int timeoutMillis = 1000)
    {
        if (!TryGetAvailableDetector(timeoutMillis, out int detectorId))
        {
            AddCore();
            if (!TryGetAvailableDetector(timeoutMillis, out detectorId))
                throw new TimeoutException("Timeout waiting for available detector");
        }

        try
        {
            return detectCores[detectorId].ComputeBlackPercent(image) > blackThresholds;
        }
        finally
        {
            available.Enqueue(detectorId); // 释放
        }
    }

    private bool TryGetAvailableDetector(int timeoutMillis, out int id)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        while (stopwatch.ElapsedMilliseconds < timeoutMillis)
        {
            if (available.TryDequeue(out id))
                return true;
            Thread.Sleep(10);
        }

        id = -1;
        return false;
    }

    private void AddCore()
    {
        lock (lockObj)
        {
            if (detectCores.Count >= maxSize)
                throw new InvalidOperationException("Reach max detectors in pool");

            var newDetector = new DetectCore(modelAsset, classNames);
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
                if (id >= 0 && id < detectCores.Count)
                {
                    detectCores[id].Dispose();
                    detectCores[id] = null; // 标记为空，避免再访问
                    activeCount--;
                    return true;
                }
            }
            return false; // 没有可移除的空闲 detector
        }
    }

    public int PoolSize => activeCount;


    public int BusyCount => PoolSize - available.Count;

    public void Dispose()
    {
        foreach (var detector in detectCores)
        {
            detector?.Dispose();
        }
    }
}
