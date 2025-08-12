using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using Unity.Sentis;
using UnityEngine;
using UnityEngine.UI;

namespace test
{


    public class WindowClient : MonoBehaviour
    {
        public RawImage display;
        public Text resultText;
        public ModelAsset modelAsset;
        private Texture2D tex;
        private int expectedWidth = 480;
        private int expectedHeight = 320;
        // private SentisTester detectorTester;
        private Detector detector;
        private IntPtr hwnd;
        private float timer = 0f;
        private Texture2D srcTex;

        private List<string> classNames = new List<string> {
        "dialogue",
        "RSE_s",
        "RSE_ns",
        "shiny_star",
        "next",
        "can_run",
        "FrLg_s",
        "FrLg_ns",
        "before_enter",
        "bite_eng",
        "fish_gone_eng",
        "get_fish_eng",
        "no_fish_eng",
        "bite_jpn",
        "fish_gone_jpn",
        "get_fish_jpn",
        "no_fish_jpn",
        };

        private Dictionary<string, float> classThresholds = new Dictionary<string, float>
{
    // { "black", 0.8f },
    { "dialogue", 0.6f },
    { "RSE_s", 0.5f },
    { "RSE_ns", 0.5f },
    { "shiny_star", 0.6f },
    { "next", 0.25f },
    { "can_run", 0.5f },
    { "FrLg_s", 0.5f },
    { "FrLg_ns", 0.5f },
    { "before_enter", 0.8f },
    { "bite_eng", 0.5f },
    { "fish_gone_eng", 0.5f },
    { "get_fish_eng", 0.45f },
    { "no_fish_eng", 0.5f },
    { "bite_jpn", 0.5f },
    { "fish_gone_jpn", 0.5f },
    { "get_fish_jpn", 0.6f },
    { "no_fish_jpn", 0.5f },
};

        void Awake()
        {
            display.rectTransform.localScale = new Vector3(1, -1, 1);
            tex = new Texture2D(expectedWidth, expectedHeight, TextureFormat.RGB24, false);
            // detectorTester = GetComponent<SentisTester>();
            detector = Detector.Init(initSize: 1, maxSize: 2, modelAsset: modelAsset, classThresholds: classThresholds);

            List<IntPtr> windows = Win32Utils.FindDesktopChildWindowsWithText("BongoCat");
            if (windows.Count == 0)
            {
                Debug.LogWarning("❌ 未找到目标窗口");
                return;
            }

            hwnd = windows[0];
        }

        void Update()
        {
            if (timer <= 0f)
            {
                RequestRawScreenshot();
                timer = 1f / 60f;
            }
            timer -= Time.deltaTime;
        }

        void RequestRawScreenshot()
        {
            try
            {
                float t0 = Time.realtimeSinceStartup;

                byte[] rawData = Win32Utils.CaptureWindow(hwnd, out int srcWidth, out int srcHeight);
                float t1 = Time.realtimeSinceStartup;
                // 测试按键功能
                Win32Utils.SendKey(hwnd, KeyCode.A);

                // 加载原始尺寸截图
                srcTex = new Texture2D(srcWidth, srcHeight, TextureFormat.RGB24, false);
                srcTex.LoadRawTextureData(rawData);
                srcTex.Apply();

                // // ✅ 使用 RenderTexture 缩放为 480x320
                // RenderTexture rt = RenderTexture.GetTemporary(expectedWidth, expectedHeight);
                // Graphics.Blit(srcTex, rt);  // 📌 不用 DrawTexture

                // RenderTexture.active = rt;
                // tex.Reinitialize(expectedWidth, expectedHeight);
                // tex.ReadPixels(new Rect(0, 0, expectedWidth, expectedHeight), 0, 0);
                // tex.Apply();

                // RenderTexture.active = null;
                // RenderTexture.ReleaseTemporary(rt);

                // ✅ 显示图像
                if (display != null)
                {
                    display.texture = srcTex;
                }


                float t2 = Time.realtimeSinceStartup;

                Dictionary<string, float> result = detector.RawDetect(rawData, detectBlack: true);
                float t3 = Time.realtimeSinceStartup;

                float captureTime = (t1 - t0) * 1000f;
                float preDetectTime = (t2 - t1) * 1000f;
                float detectTime = (t3 - t2) * 1000f;
                float totalTime = (t3 - t0) * 1000f;
                float fps = 1000f / totalTime;

                StringBuilder sb = new StringBuilder();
                foreach (var kv in result)
                {
                    sb.AppendLine($"{kv.Key}: {kv.Value:P1}");
                }
                sb.AppendLine($"⏱ Capture: {captureTime:F1} ms, PreDetect: {preDetectTime:F1} ms, Detect: {detectTime:F1} ms, Total: {totalTime:F1} ms | FPS: {fps:F1}");

                if (resultText != null)
                    resultText.text = sb.ToString();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Exception] {ex.Message}\n{ex.StackTrace}");
            }
        }
    }

}