using System.Collections;
using System.Collections.Generic;
using Unity.Sentis;
using UnityEngine;
using UnityEngine.Rendering;

public class DetectCore
{
    private readonly List<string> classNames;
    private readonly int numClasses;
    private Worker worker;
    private readonly int width;
    private readonly int height;
    private readonly int area;
    private TensorShape tensorShape;


    public DetectCore(ModelAsset modelAsset, List<string> classNames, int width = 480, int height = 320, bool? useGPU = null)
    {
        useGPU ??= SystemInfo.supportsComputeShaders && SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null;

        var runtimeModel = ModelLoader.Load(modelAsset);
        worker = new Worker(runtimeModel, (bool)useGPU ? BackendType.GPUCompute : BackendType.CPU);
        this.classNames = classNames;
        numClasses = classNames.Count;
        this.width = width;
        this.height = height;
        area = width * height;
        tensorShape = new TensorShape(1, 3, height, width);
    }

    public Tensor<float> Preprocess(byte[] rawData)
    {
        float[] chwData = new float[3 * area];

        for (int i = 0; i < area; i++)
        {
            byte r = rawData[i * 3 + 0];
            byte g = rawData[i * 3 + 1];
            byte b = rawData[i * 3 + 2];

            chwData[0 * area + i] = b;
            chwData[1 * area + i] = g;
            chwData[2 * area + i] = r;
        }
        return new Tensor<float>(tensorShape, chwData);
    }

    public Dictionary<string, float> Run(byte[] rawData, bool includeBlack = false)
    {
        using Tensor<float> tensor = Preprocess(rawData);
        worker.Schedule(tensor);
        using Tensor<float> outputRaw = worker.PeekOutput() as Tensor<float>;
        using Tensor<float> output = outputRaw.ReadbackAndClone();

        float[] maxScores = new float[numClasses];
        for (int i = 0; i < numClasses; i++)
            maxScores[i] = float.MinValue;
        int numAnchors = output.shape[1];

        for (int anchorIdx = 0; anchorIdx < numAnchors; anchorIdx++)
        {
            for (int c = 0; c < numClasses; c++)
            {
                float score = output[0, anchorIdx, c];
                if (score > maxScores[c])
                    maxScores[c] = score;
            }
        }

        var result = new Dictionary<string, float>();
        for (int i = 0; i < numClasses; i++)
        {
            result[classNames[i]] = maxScores[i];
        }

        if (includeBlack)
        {
            result["black"] = ComputeBlackPercent(rawData);
        }

        return result;
    }


    public float ComputeBlackPercent(byte[] rawData, float threshold = 10f)
    {
        int blackCount = 0;
        int totalPixels = rawData.Length / 3;

        for (int i = 0; i < totalPixels; i++)
        {
            byte r = rawData[i * 3 + 0];
            byte g = rawData[i * 3 + 1];
            byte b = rawData[i * 3 + 2];

            float gray = 0.299f * r + 0.587f * g + 0.114f * b;
            if (gray < threshold)
                blackCount++;
        }

        return (float)blackCount / totalPixels;
    }

    public void Dispose()
    {
        worker?.Dispose();
    }
}