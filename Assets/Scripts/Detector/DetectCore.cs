using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Sentis;
using UnityEngine;
using UnityEngine.Rendering;

public class DetectCore : IDisposable
{
    private readonly List<DetectionClass> classEnums;
    private readonly int numClasses;

    private readonly int width;
    private readonly int height;
    private readonly int area;
    private readonly TensorShape tensorShape;

    // Sentis worker：仅在主线程使用（GPU 或 CPU 后端）
    private readonly Worker worker;

    // 路线C：主线程处理的 GPU 请求队列
    private readonly Queue<Request> gpuQueue = new Queue<Request>();

    private struct Request
    {
        public Tensor<float> InputTensor;   // 主/后台创建，但仅在主线程送入 worker
        public float? BlackPercent;         // 可选的黑像素比例（后台CPU计算）
        public TaskCompletionSource<Dictionary<DetectionClass, float>> Tcs;
    }

    public DetectCore(ModelAsset modelAsset, List<DetectionClass> classEnums, int width = 480, int height = 320, bool? useGPU = null)
    {
        useGPU ??= SystemInfo.supportsComputeShaders && SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null;

        var runtimeModel = ModelLoader.Load(modelAsset);
        worker = new Worker(runtimeModel, useGPU.Value ? BackendType.GPUCompute : BackendType.CPU);

        this.classEnums = classEnums;
        numClasses = classEnums.Count;
        this.width = width;
        this.height = height;
        area = width * height;
        tensorShape = new TensorShape(1, 3, height, width);
    }

    // —— 纯CPU预处理（可在后台线程调用；不要触碰 UnityEngine 对象） —— //
    public Tensor<float> Preprocess(byte[] rawData)
    {
        float[] chw = new float[3 * area];
        for (int i = 0; i < area; i++)
        {
            byte r = rawData[i * 3 + 0];
            byte g = rawData[i * 3 + 1];
            byte b = rawData[i * 3 + 2];
            // 根据你的模型通道顺序（此处示例 B,G,R）
            chw[0 * area + i] = b;
            chw[1 * area + i] = g;
            chw[2 * area + i] = r;
        }
        return new Tensor<float>(tensorShape, chw);
    }

    // —— 纯CPU：计算黑像素比例 —— //
    public float ComputeBlackPercent(byte[] rawData, float threshold = 20f)
    {
        int black = 0;
        int total = rawData.Length / 3;
        for (int i = 0; i < total; i++)
        {
            byte r = rawData[i * 3 + 0];
            byte g = rawData[i * 3 + 1];
            byte b = rawData[i * 3 + 2];
            float gray = 0.299f * r + 0.587f * g + 0.114f * b;
            if (gray < threshold) black++;
        }
        return (float)black / total;
    }

    // —— 后处理（主线程 Tensor 版，给同步快路径用） —— //
    private Dictionary<DetectionClass, float> Postprocess(Tensor<float> output, float? blackPercentOpt)
    {
        int numAnchors = output.shape[1];

        float[] maxScores = new float[numClasses];
        for (int i = 0; i < numClasses; i++)
            maxScores[i] = float.NegativeInfinity;

        for (int a = 0; a < numAnchors; a++)
        {
            for (int c = 0; c < numClasses; c++)
            {
                float s = output[0, a, c]; // 注意：在主线程读取
                if (s > maxScores[c]) maxScores[c] = s;
            }
        }

        var result = new Dictionary<DetectionClass, float>(numClasses + (blackPercentOpt.HasValue ? 1 : 0));
        for (int i = 0; i < numClasses; i++) result[classEnums[i]] = maxScores[i];
        if (blackPercentOpt.HasValue) result[DetectionClass.Black] = blackPercentOpt.Value;
        return result;
    }

    // —— 后处理（后台线程用：基于 float[] 的纯托管实现，可跨线程） —— //
    private Dictionary<DetectionClass, float> PostprocessFlat(float[] flat, int numAnchors, float? blackPercentOpt)
    {
        // flat 形状: [numAnchors * numClasses]，索引顺序 (anchor, class)
        var maxScores = new float[numClasses];
        for (int i = 0; i < numClasses; i++) maxScores[i] = float.NegativeInfinity;

        int idx = 0;
        for (int a = 0; a < numAnchors; a++)
        {
            for (int c = 0; c < numClasses; c++, idx++)
            {
                float s = flat[idx];
                if (s > maxScores[c]) maxScores[c] = s;
            }
        }

        var result = new Dictionary<DetectionClass, float>(numClasses + (blackPercentOpt.HasValue ? 1 : 0));
        for (int i = 0; i < numClasses; i++) result[classEnums[i]] = maxScores[i];
        if (blackPercentOpt.HasValue) result[DetectionClass.Black] = blackPercentOpt.Value;
        return result;
    }

    // —— 路线C：异步入口（任何线程均可调用；后台做预处理/黑像素，主线程 Pump 做GPU） —— //
    public Task<Dictionary<DetectionClass, float>> DetectAsync(byte[] rawData, bool includeBlack = false)
    {
        var tcs = new TaskCompletionSource<Dictionary<DetectionClass, float>>(TaskCreationOptions.RunContinuationsAsynchronously);

        Task.Run(() =>
        {
            Tensor<float> input = null;
            try
            {
                input = Preprocess(rawData);
                float? black = includeBlack ? ComputeBlackPercent(rawData) : (float?)null;

                lock (gpuQueue)
                {
                    gpuQueue.Enqueue(new Request
                    {
                        InputTensor = input,
                        BlackPercent = black,
                        Tcs = tcs
                    });
                }
            }
            catch (Exception e)
            {
                try { input?.Dispose(); } catch { }
                tcs.TrySetException(e);
            }
        });

        return tcs.Task;
    }

    // —— 主线程每帧调用，执行少量GPU调度+读回；再把“纯托管数组”交给后台线程后处理 —— //
    public void PumpOnce(int maxPerFrame = 2)
    {
        if (!MainThreadDispatcher.IsMainThread) return;

        for (int i = 0; i < maxPerFrame; i++)
        {
            Request req;
            lock (gpuQueue)
            {
                if (gpuQueue.Count == 0) break;
                req = gpuQueue.Dequeue();
            }

            try
            {
                using (req.InputTensor)
                {
                    // 1) 推理（主线程）
                    worker.Schedule(req.InputTensor);
                    using var outGpu = worker.PeekOutput() as Tensor<float>;
                    using var outputCPU = outGpu.ReadbackAndClone(); // 仍在主线程

                    // 2) 在主线程把 Tensor 拷贝成 float[]（避免后台线程读 Tensor）
                    int numAnchors = outputCPU.shape[1];
                    var flat = new float[numAnchors * numClasses];
                    int p = 0;
                    for (int a = 0; a < numAnchors; a++)
                    {
                        for (int c = 0; c < numClasses; c++)
                        {
                            flat[p++] = outputCPU[0, a, c]; // 主线程读取
                        }
                    }

                    // 3) 后台线程做 reduce-max 和结果打包
                    Task.Run(() =>
                    {
                        try
                        {
                            var res = PostprocessFlat(flat, numAnchors, req.BlackPercent);
                            req.Tcs.TrySetResult(res);
                        }
                        catch (Exception e)
                        {
                            req.Tcs.TrySetException(e);
                        }
                        // 无需显式 Dispose，outputCPU 已在 using 中释放
                    });
                }
            }
            catch (Exception e)
            {
                req.Tcs.TrySetException(e);
            }
        }
    }

    // —— 主线程同步快路径（当调用点在主线程且希望立即返回时使用） —— //
    public Dictionary<DetectionClass, float> DetectSyncOnMainThread(byte[] rawData, bool includeBlack = false)
    {
        if (!MainThreadDispatcher.IsMainThread)
            throw new InvalidOperationException("DetectSyncOnMainThread must be called from main thread.");

        float? black = includeBlack ? ComputeBlackPercent(rawData) : (float?)null;

        using var input = Preprocess(rawData);
        worker.Schedule(input);
        using var outGpu = worker.PeekOutput() as Tensor<float>;
        using var outputCPU = outGpu.ReadbackAndClone();

        // 同步快路径：直接在主线程做 Postprocess（读取 Tensor 安全）
        return Postprocess(outputCPU, black);
    }

    public void Dispose()
    {
        worker?.Dispose();
    }
}
