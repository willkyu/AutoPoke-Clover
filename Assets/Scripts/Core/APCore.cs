using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Sentis;
using UnityEngine;
using UnityEngine.UI;



public class APCore : MonoBehaviour
{
    // Start is called before the first frame update
    List<IntPtr> windows;
    List<Task> tasks;
    public ModelAsset modelAsset;

    private Detector detector;

    [Header("Alpha")]
    public string alphaMode = "Wilds";
    public bool LR = true;
    public Text LR_btn_text;
    private AlphaFrLgStarters alphaFrLgStarters;
    private AlphaWilds alphaWilds;



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


    private void Awake()
    {
        Screen.SetResolution(400, 400, FullScreenMode.Windowed);
    }
    void Start()
    {
        detector = Detector.Init(initSize: 1, maxSize: 2, modelAsset: modelAsset, classThresholds: classThresholds);
        GetWindows();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void GetWindows()
    {
        windows = Win32Utils.FindDesktopChildWindowsWithText(Settings.General.windowName);
        if (windows.Count == 0)
        {
            Debug.LogWarning("❌ 未找到目标窗口");
            return;
        }
        // hwnd = windows[0];
    }

    public void AlphaRun()
    {
        switch (alphaMode)
        {
            case "FrLgStarters":

                alphaFrLgStarters = new AlphaFrLgStarters(detector);
                foreach (IntPtr hwnd in windows)
                {
                    alphaFrLgStarters.Run(hwnd);
                }
                break;
            case "Wilds":
                alphaWilds = new AlphaWilds(detector, LR);
                foreach (IntPtr hwnd in windows)
                {
                    alphaWilds.Run(hwnd);
                }
                break;
        }
    }

    public void AlphaLR()
    {
        LR = !LR;
        LR_btn_text.text = LR ? "左右走" : "上下走";
    }

    public void AlphaEnd()
    {
        alphaFrLgStarters?.End();
        alphaWilds?.End();

    }

    private void OnApplicationQuit()
    {
        alphaFrLgStarters?.End();
        alphaWilds?.End();
#if !UNITY_EDITOR
        System.Diagnostics.Process.GetCurrentProcess().Kill();//当前杀死进程
#endif
    }
}
