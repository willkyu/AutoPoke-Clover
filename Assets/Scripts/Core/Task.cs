using System;
using System.Collections.Generic;
using System.Threading;

public enum Language
{
    Jpn, Eng
}

public enum GameVersion
{
    RS, E, FrLg
}

public enum TaskMode
{
    Single, Multiple
}

public enum Function
{
    Move, Stationary, Fish
}



public class Task
{

    public List<IntPtr> windows;  // 窗口句柄
    public int counter = 0;
    public Language language = Language.Eng;
    public GameVersion gameVersion = GameVersion.RS;
    public TaskMode taskMode = TaskMode.Multiple;
    public Function function = Function.Move;
    public float speed = 1f;

    private Detector detector;
    private ControlUtils ctrl;
    private List<Thread> threads = new List<Thread> { };


    public Task(TaskMode tm)
    {
        taskMode = tm;
    }


}
