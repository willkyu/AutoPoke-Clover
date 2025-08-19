using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;


public interface TaskCore
{
    public bool Exe();
}

public class GeneralCore : TaskCore
{
    private IntPtr hwnd;
    private Detector detector;
    private ControlUtils ctrl;
    private List<DetectionClass> detectRes;
    private float speed = 1f;
    private float hitDuration = 0.1f;
    private GameVersion gameVersion = GameVersion.RS;
    private readonly System.Random rand = new System.Random();

    // ------------------ Key Arrays ------------------
    private GameKey[] SoftResetKeys = new GameKey[]
    {
        GameKey.A, GameKey.B, GameKey.Start, GameKey.Select
    };
    private GameKey[] UpDownKeys = new GameKey[]
    {
        GameKey.Up,GameKey.Down
    };
    private GameKey[] LeftRightKeys = new GameKey[]
    {
        GameKey.Left,GameKey.Right
    };
    private GameKey[] RandomKeys = new GameKey[]
    {
        GameKey.Left,GameKey.Right, GameKey.Up,GameKey.Down, GameKey.Start, GameKey.Select
    };

    // ------------------ Functions ------------------
    private void Wait(int waitTimeMS)
    {
        Thread.Sleep((int)(waitTimeMS / speed));
    }
    private void Press(GameKey key, bool wait = true)
    {
        ctrl.KeyHit(key, hitDuration);
        Wait(wait ? 100 : 0);
    }
    private void ReleaseAllKeys()
    {
        foreach (GameKey key in Enum.GetValues(typeof(GameKey)))
        {
            ctrl.KeyUp(key);
        }
    }
    private bool Detect(DetectionClass targetClass, bool detectBlack = false)
    {
        detectRes = detector.Detect(Win32Utils.CaptureWindow(hwnd, out _, out _), detectBlack);
        return detectRes.Contains(targetClass);
    }
    private bool DetectBlack(float? minRatio = null)
    {
        return detector.DetectBlack(Win32Utils.CaptureWindow(hwnd, out _, out _), minRatio);
    }
    private void WaitTillBlack(bool PressA = false) { while (!DetectBlack()) { if (PressA) Press(GameKey.A); Wait(200); } }
    private void WaitTillNotBlack() { while (DetectBlack()) Wait(200); }
    private void SoftReset()
    {
        ctrl.KeysHit(SoftResetKeys);
        Thread.Sleep(rand.Next(0, 300));
        while (!Detect(DetectionClass.BeforeEnter))
        {
            if (gameVersion == GameVersion.FrLg)
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
        if (gameVersion == GameVersion.FrLg)
        {
            Press(GameKey.B);
            Wait(500);
            Press(GameKey.B);
            Wait(500);
        }
    }

    private void Run()
    {
        Press(GameKey.Right);
        Press(GameKey.Down);
        Press(GameKey.A);
        WaitTillBlack(PressA: true);
        WaitTillNotBlack();
        Wait(1200);
    }

    private void ShinyHandle() { }

    public bool Exe()
    {
        throw new System.NotImplementedException();
    }
}