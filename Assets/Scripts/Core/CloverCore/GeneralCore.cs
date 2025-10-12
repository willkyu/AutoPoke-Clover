using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Unity.VisualScripting;
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
    protected bool repelFlag = false;

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
    protected void RandomPress(GameKey[] keys)
    {
        ctrl.KeyHit(keys[rand.Next(0, keys.Length)], config.hitDuration * 2);
        // Wait(50);
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
        var sw = Stopwatch.StartNew();
        detectRes = detector.Detect(Win32Utils.CaptureWindow(hwnd, out _, out _), detectBlack);
        sw.Stop();
        double elapsedMs = sw.Elapsed.TotalMilliseconds;

        // 计算 FPS
        double fps = elapsedMs > 0 ? 1000.0 / elapsedMs : 0.0;
        this.TriggerEvent(EventName.SetFPS, new SetFPSEventArgs { fps = fps });
        return detectRes.Contains(targetClass);
    }
    protected bool DetectBlack(float? minRatio = null)
    {
        return detector.DetectBlack(Win32Utils.CaptureWindow(hwnd, out _, out _), minRatio);
    }
    protected void WaitTillBlack(bool PressA = false) { while (!DetectBlack()) { if (PressA) Press(GameKey.A); else Wait(200); } }
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
                if (Detect(DetectionClass.BeforeEnter)) break; // Better robustness
                Wait(rand.Next(0, 500));
                if (Detect(DetectionClass.BeforeEnter)) break;
                Press(GameKey.A);
                if (Detect(DetectionClass.BeforeEnter)) break;
            }
            else
            {
                Press(GameKey.A);
                if (Detect(DetectionClass.BeforeEnter)) break;
                Wait(200);
                if (Detect(DetectionClass.BeforeEnter)) break;
            }
        }
        // Press(GameKey.A);
        WaitTillBlack(PressA: true);
        WaitTillNotBlack();
        if (config.gameVersion == GameVersion.FrLg)
        {
            Press(GameKey.B);
            Wait(500);
            Press(GameKey.B);
            Wait(2000);
        }
    }

    protected void UseRepel()
    {
        UnityEngine.Debug.Log("use repel start");
        Press(GameKey.Start); Press(GameKey.A);
        WaitTillBlack();
        WaitTillNotBlack();
        if (config.language == Language.Jpn)
        {
            // TODO collect and retrain
            Press(GameKey.A);
            Press(GameKey.A);
        }
        else
        {
            UnityEngine.Debug.Log(config.language);
            while (!Detect(DetectionClass.Dialogue)) Press(GameKey.A);
            while (Detect(DetectionClass.Dialogue)) Press(GameKey.A);
        }
        while (!DetectBlack()) Press(GameKey.B);
        WaitTillNotBlack();
        Press(GameKey.B);
        repelFlag = true;
        UnityEngine.Debug.Log("use repel end");

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
        Win32Utils.SaveWindowScreenshot(hwnd);
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
        APCore.I.ReturnWindow(this.hwnd);
    }

    public void End()
    {
        ReleaseAllKeys();
    }


}