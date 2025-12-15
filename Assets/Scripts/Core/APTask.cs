using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public enum Language { Jpn, Eng }
public enum GameVersion { RS, E, FrLg }
public enum TaskMode { Single, Multiple }
public enum Function { Move, Stationary, Fish }
public enum StationaryMode { FrLgStarters, RSEStarters, NormalHitA, Gift }

[Serializable]
public struct TaskParams
{
    public Language language;
    public GameVersion gameVersion;
    public TaskMode taskMode;
    public Function function;
    public float speed;
    public int hitDuration;
    public int counter;

    // Stationary
    public StationaryMode stationaryMode;
    // Move
    public bool run;
    public bool jump;
    public bool sweetScent;
    public bool repel;
    public bool ifLR;
    // Extra
    public int extraData;

    public TaskParams(
        Language lang, GameVersion gv, TaskMode mode, Function fn,
        float spd, int hd, int cnt, StationaryMode sMode, bool r, bool j, bool ss, bool re, bool lr, int extra)
    {
        language = lang;
        gameVersion = gv;
        taskMode = mode;
        function = fn;
        speed = spd;
        hitDuration = hd;
        counter = cnt;
        stationaryMode = sMode;
        run = r;
        jump = j;
        sweetScent = ss;
        repel = re;
        ifLR = lr;
        extraData = extra;
    }
}

public class APTask : MonoBehaviour
{
    public readonly Guid TaskId = Guid.NewGuid();
    // public RingProgressBar rpb;
    [Header("UI-绑定参数（可由 UI 修改）")]
    public Language language = Language.Eng;
    public GameVersion gameVersion = GameVersion.RS;
    public TaskMode taskMode = TaskMode.Multiple;
    public Function function = Function.Move;
    [Range(0.5f, 3f)] public float speed = 1f;
    public int hitDuration = 100;
    public int counter = 0;

    // Stationary
    public StationaryMode stationaryMode;
    // Move
    public bool run = false;
    public bool jump = false;
    public bool sweetScent = false;
    public bool repel = false;
    public bool lr = true;
    // Extra
    public int extraData = 0;

    [Header("状态只读")]
    [SerializeField] private bool running;
    public bool IsRunning => running;

    // —— 事件（UI 可订阅以刷新显示）
    // public event Action<int> OnCounterChanged;
    // public event Action<TaskParams> OnParamsChanged;
    // public event Action<bool> OnRunningChanged;

    // —— 运行态
    private readonly List<TaskCore> cores = new();
    private readonly List<Thread> threads = new();
    private List<IntPtr> wins = new();

    // 存活线程计数 & 防止重复触发
    private int aliveCount = 0;
    private int stoppedSignal = 0;  // 0=未触发过停止事件，1=已触发

    // ===== 参数快照 =====
    private TaskParams CurrentParams() =>
        new TaskParams(language, gameVersion, taskMode, function, speed, hitDuration, counter, stationaryMode, run, jump, sweetScent, repel, lr, extraData);



    // public void IncCounter() => MainThreadDispatcher.Enqueue(() => SetCounter(counter + 1));
    // public void DecCounter() => MainThreadDispatcher.Enqueue(() => SetCounter(counter - 1));
    public void SetCounter(int value)
    {
        counter = value;
        Debug.Log($"update count: {value}");
        // rpb.value = value;
        // OnParamsChanged?.Invoke(CurrentParams());
    }
    public void SetHwnd(List<IntPtr> hwnds) { wins = hwnds; }

    // ===== 生命周期 =====
    private void OnDestroy() => StopTask();

    // ===== Start / Stop =====
    public void StartTask()
    {
        if (taskMode == TaskMode.Multiple) { wins = APCore.I.RentAllWindows(); Debug.Log(wins.ToString()); }

        if (wins.Count == 0)
        {
            Debug.LogWarning("[APTask] No Specific Window Selected.");
            return;
        }

        // 参数快照
        var snapshot = CurrentParams();
        cores.Clear();
        threads.Clear();

        // 重置计数与信号
        aliveCount = 0;
        Interlocked.Exchange(ref stoppedSignal, 0);

        // 每个窗口起一个线程
        foreach (var hwnd in wins)
        {
            var ctrl = new ControlUtils(hwnd);

            // 通过 CoreFactory 注入依赖，生成实现了 TaskCore 的实例
            TaskCore core = CoreFactory.GenerateCore(this, CurrentParams(), hwnd);
            cores.Add(core);

            /// 每启一个线程，计数 +1
            Interlocked.Increment(ref aliveCount);

            // 包装一个入口，保证退出时计数 -1
            var th = new Thread(() =>
            {
                try
                {
                    core.Exe();
                }
                catch (ThreadInterruptedException) { /* StopTask 时可能触发，忽略 */ }
                // catch (System.Exception e) { Debug.LogException(e); }
                finally
                {
                    if (Interlocked.Decrement(ref aliveCount) == 0)
                    {
                        // 最后一个线程退出 → 统一收尾
                        OnAllThreadsExited();
                    }
                }
            })
            {
                IsBackground = true,
                Name = $"APTask-Core-{hwnd}"
            };
            threads.Add(th);
            th.Start();
        }

        running = true;
        // OnRunningChanged?.Invoke(true);
        Debug.Log($"[APTask] Started with {wins.Count} window(s).");

        this.TriggerEvent(EventName.SetRunning, new SetRunningEventArgs { running = true });


    }

    public void StopTask()
    {
        if (!running) return;


        // 请求退出并等待
        foreach (var th in threads)
        {
            if (th == null) continue;
            if (!th.Join(0))
            {
                try { th.Interrupt(); } catch { }
            }
        }

        foreach (GeneralCore core in cores)
        {
            core.End();
        }
        threads.Clear();
        cores.Clear();

        // 归还窗口
        if (APCore.I != null)
        {
            foreach (var h in wins) APCore.I.ReturnWindow(h);
        }
        wins.Clear();

        // 如果线程自己已经触发了收尾，就不要重复发事件
        if (Interlocked.Exchange(ref stoppedSignal, 1) == 0)
        {
            running = false;
            Debug.Log("[APTask] Stopped (manual).");
            // 事件最好在主线程发
            MainThreadDispatcher.Enqueue(() =>
            {
                this.TriggerEvent(EventName.SetRunning, new SetRunningEventArgs { running = false });
            });
        }
    }

    // 新增：所有线程自然结束时的统一收尾
    private void OnAllThreadsExited()
    {
        // 避免重复触发（与 StopTask 的收尾竞争）
        if (Interlocked.Exchange(ref stoppedSignal, 1) != 0) return;

        // 清理与归还在主线程做，避免触 Unity 对象的线程问题
        MainThreadDispatcher.Enqueue(() =>
        {
            // 归还窗口
            if (APCore.I != null)
            {
                foreach (var h in wins) APCore.I.ReturnWindow(h);
            }
            wins.Clear();
            threads.Clear();
            cores.Clear();

            running = false;
            Debug.Log("[APTask] All cores exited (natural).");
            this.TriggerEvent(EventName.SetRunning, new SetRunningEventArgs { running = false });
        });
    }

    // （可选）对外查看
    public IReadOnlyList<IntPtr> RentedWindows => wins;

    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// </summary>
    void Awake()
    {
        APCore.I.AddTask(TaskId, this);
    }
}
