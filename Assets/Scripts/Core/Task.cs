using System;

public class Task
{
    public IntPtr hwnd;  // 窗口句柄
    public int counter = 0;
    public bool isEnabled = true;

    // 自定义任务参数（根据你的刷闪逻辑调整）
    public string targetPokemon;
    public bool useRepel;
    public int maxAttempts;

    // 可以挂载委托或行为树来定义不同任务逻辑
    public Action<Task> ExecuteStep;

    public Task(IntPtr window)
    {
        hwnd = window;
    }

    public void RunStep()
    {
        if (isEnabled && ExecuteStep != null)
        {
            ExecuteStep(this);
            counter++;
        }
    }
}
