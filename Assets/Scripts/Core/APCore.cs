using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Sentis;
using UnityEngine;
using UnityEngine.UI;




public class APCore : MonoBehaviour
{
    private static APCore instance;
    private static readonly object locker = new object();
    private static bool applicationIsQuitting = false;
    public static APCore I
    {
        get
        {
            if (applicationIsQuitting) return instance;
            if (instance == null)
            {
                lock (locker)
                {

                    instance = FindObjectOfType<APCore>();
                    if (instance == null)
                    {
                        // 如果场景中没有，就新建一个 GameObject 并挂载
                        var go = new GameObject("__APCore");
                        instance = go.AddComponent<APCore>();
                        DontDestroyOnLoad(go);
                    }
                }
            }
            return instance;
        }
    }

    [Header("Model")]
    public ModelAsset modelAsset;
    [Header("Window")]



    private readonly Dictionary<DetectionClass, float> classThresholds =
        new Dictionary<DetectionClass, float>
    {
    { DetectionClass.Dialogue, 0.5f },
    { DetectionClass.BlankDialogue, 0.5f },
    { DetectionClass.RSE_s, 0.5f },
    { DetectionClass.RSE_ns, 0.5f },
    { DetectionClass.ShinyStar, 0.6f },
    { DetectionClass.Next, 0.25f },
    { DetectionClass.CanRun, 0.5f },
    { DetectionClass.FrLg_s, 0.5f },
    { DetectionClass.FrLg_ns, 0.5f },
    { DetectionClass.BeforeEnter, 0.8f },
    { DetectionClass.Options, 0.5f },
    // { DetectionClass.BiteEng, 0.5f },
            // { DetectionClass.FishGoneEng, 0.7f },
            // { DetectionClass.GetFishEng, 0.5f },
            // { DetectionClass.NoFishEng, 0.7f },
            // { DetectionClass.BiteJpn, 0.5f },
            // { DetectionClass.FishGoneJpn, 0.6f },
            // { DetectionClass.GetFishJpn, 0.6f },
            // { DetectionClass.NoFishJpn, 0.5f },
    };


    // —— 运行态
    private Detector detector;
    private readonly List<IntPtr> allWindows = new();
    private readonly Dictionary<IntPtr, bool> windowBusy = new(); // true = 被占用

    private Dictionary<Guid, APTask> tasks = new();


    void Awake()
    {
        // if (instance != null) { Destroy(gameObject); return; }
        // instance = this;
        DontDestroyOnLoad(gameObject);
        Screen.SetResolution(600, 450, FullScreenMode.Windowed);
        EventManager.I.AddListener(EventName.SetCounter, SetCounter);
    }

    void Start()
    {
        RefreshWindows();
        detector = Detector.Init(initSize: 1, maxSize: Math.Max(2, allWindows.Count),
                                 modelAsset: modelAsset, classThresholds: classThresholds);
    }
    private void OnDestroy()
    {
        EventManager.I.RemoveListener(EventName.SetCounter, SetCounter);
        applicationIsQuitting = true;
    }

    void OnApplicationQuit()
    {
        Destroy(this);
        detector?.Dispose();
    }

    // ======= Task管理 =======
    public void AddTask(Guid guid, APTask apTask) { tasks[guid] = apTask; }
    public void DelTask(Guid guid) { tasks.Remove(guid); }

    public void SetCounter(object sender, EventArgs args)
    {
        var data = args as SetCounterEventArgs;
        MainThreadDispatcher.Enqueue(() => tasks[data.guid].SetCounter(data.count));
    }



    // ======= 窗口管理 =======

    public void RefreshWindows()
    {
        allWindows.Clear();
        allWindows.AddRange(Win32Utils.FindDesktopChildWindowsWithText(Settings.General.windowName));
        windowBusy.Clear();
        foreach (var h in allWindows) windowBusy[h] = false;
        Debug.Log($"[APCore] Found windows: {allWindows.Count}");
        this.TriggerEvent(EventName.SetWinCount, new SetWinCountEventArgs { winCount = allWindows.Count });
    }

    /// <summary>租借一个空闲窗口；失败返回 IntPtr.Zero</summary>
    public bool RentWindow(IntPtr hwnd)
    {
        if (windowBusy.ContainsKey(hwnd) && !windowBusy[hwnd])
        {
            windowBusy[hwnd] = true;
            return true;
        }
        return false;
    }

    /// <summary>租借所有空闲窗口</summary>
    public List<IntPtr> RentAllWindows()
    {
        List<IntPtr> res = new();
        foreach (IntPtr hwnd in allWindows)
        {
            if (!windowBusy[hwnd]) { windowBusy[hwnd] = true; res.Add(hwnd); }
        }
        // Debug.Log(res.Count);
        return res;
    }

    /// <summary>归还窗口；非法句柄会被忽略</summary>
    public void ReturnWindow(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return;
        if (windowBusy.ContainsKey(hwnd)) windowBusy[hwnd] = false;
    }

    public IReadOnlyList<IntPtr> AllWindows => allWindows;
    public bool IsBusy(IntPtr hwnd) => windowBusy.TryGetValue(hwnd, out var b) && b;

    // ======= Detector 服务 =======

    public Detector GetDetector() => detector;

    public void TestNotification()
    {
        ToastService.Test();
        MailService.Test();
    }

    // /// <summary>在需要时更换阈值、模型等（可选）。</summary>
    // public void ReinitDetector(Dictionary<DetectionClass, float> thresholds = null, ModelAsset model = null)
    // {
    //     detector?.Dispose();
    //     if (thresholds != null) classThresholds = thresholds;
    //     if (model != null) modelAsset = model;

    //     detector = Detector.Init(initSize: 1,
    //                              maxSize: Math.Max(2, allWindows.Count),
    //                              modelAsset: modelAsset,
    //                              classThresholds: classThresholds);
    // }
}