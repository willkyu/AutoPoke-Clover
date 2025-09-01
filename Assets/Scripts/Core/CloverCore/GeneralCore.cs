using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;


public interface TaskCore
{
    public void Exe();
}

public class GeneralCore : TaskCore
{
    protected IntPtr hwnd;
    protected TaskParams config;
    protected APTask owner;
    protected Detector detector;
    protected ControlUtils ctrl;
    protected readonly System.Random rand = new System.Random();
    protected List<DetectionClass> detectRes;

    // ------------------ Key Arrays ------------------
    protected GameKey[] SoftResetKeys = new GameKey[]
    {
        GameKey.A, GameKey.B, GameKey.Start, GameKey.Select
    };
    protected GameKey[] UpDownKeys = new GameKey[]
    {
        GameKey.Up,GameKey.Down
    };
    protected GameKey[] LeftRightKeys = new GameKey[]
    {
        GameKey.Left,GameKey.Right
    };
    protected GameKey[] RandomKeys = new GameKey[]
    {
        GameKey.Left,GameKey.Right, GameKey.Up,GameKey.Down, GameKey.Start, GameKey.Select
    };

    public GeneralCore(IntPtr hwnd, APTask owner, TaskParams config)
    {
        this.hwnd = hwnd;
        this.owner = owner;
        this.config = config;
        detector = APCore.I.GetDetector();
        ctrl = new ControlUtils(this.hwnd, config.speed);
    }


    // ------------------ Functions ------------------
    protected void Wait(int waitTimeMS)
    {
        Thread.Sleep((int)(waitTimeMS / config.speed));
    }
    protected void Press(GameKey key, bool wait = true)
    {
        // Win32Utils.PressKey(hwnd, Settings.Keys.GetKey(key));

        ctrl.KeyHit(key, config.hitDuration);
        Wait(wait ? 200 : 0);
    }
    protected void ReleaseAllKeys()
    {
        foreach (GameKey key in Enum.GetValues(typeof(GameKey)))
        {
            ctrl.KeyUp(key);
        }
    }
    protected bool Detect(DetectionClass targetClass, bool detectBlack = false)
    {
        detectRes = detector.Detect(Win32Utils.CaptureWindow(hwnd, out _, out _), detectBlack);
        return detectRes.Contains(targetClass);
    }
    protected bool DetectBlack(float? minRatio = null)
    {
        return detector.DetectBlack(Win32Utils.CaptureWindow(hwnd, out _, out _), minRatio);
    }
    protected void WaitTillBlack(bool PressA = false) { while (!DetectBlack()) { if (PressA) Press(GameKey.A); Wait(200); } }
    protected void WaitTillNotBlack() { while (DetectBlack()) Wait(200); Wait(300); }
    protected void SoftReset()
    {
        ctrl.KeysHit(SoftResetKeys);
        Thread.Sleep(rand.Next(0, 300));
        while (!Detect(DetectionClass.BeforeEnter))
        {
            if (config.gameVersion == GameVersion.FrLg)
            {
                Press(RandomKeys[rand.Next(0, RandomKeys.Length)], wait: false);
                Wait(rand.Next(0, 500));
                Press(GameKey.A);
            }
            else
            {
                Press(GameKey.A);
                Wait(200);
            }
        }
        Press(GameKey.A);
        WaitTillBlack();
        WaitTillNotBlack();
        if (config.gameVersion == GameVersion.FrLg)
        {
            Press(GameKey.B);
            Wait(500);
            Press(GameKey.B);
            Wait(500);
        }
    }

    protected void Run()
    {
        Press(GameKey.Right);
        Press(GameKey.Down);
        Press(GameKey.A);
        WaitTillBlack(PressA: true);
        WaitTillNotBlack();
        Wait(1200);
    }


    protected void ShinyHandle()
    {
        // TODO
    }

    protected virtual void Encounter() { throw new System.NotImplementedException(); }
    protected virtual bool ShinyDetect() { throw new System.NotImplementedException(); }
    protected virtual void AfterDetect() { throw new System.NotImplementedException(); }

    public void Exe()
    {
        while (true)
        {
            Encounter();
            this.TriggerEvent(EventName.SetCounter, new SetCounterEventArgs { guid = owner.TaskId, count = owner.counter + 1 });
            if (ShinyDetect()) { ShinyHandle(); break; }
            AfterDetect();
        }
    }
}