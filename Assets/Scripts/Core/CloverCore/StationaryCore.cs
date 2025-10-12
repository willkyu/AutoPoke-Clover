using System;
using Unity.VisualScripting;
using UnityEngine;

public class FrLgStartersCore : GeneralCore
{
    public FrLgStartersCore(IntPtr hwnd, APTask owner, TaskParams config) : base(hwnd, owner, config) { }

    protected override void Encounter()
    {
        Debug.Log($"encounter hit duration: {config.hitDuration}");
        Press(GameKey.A);
        for (int i = 0; i < 15; i++) { Press(GameKey.A); Wait(200); }
        while (Detect(DetectionClass.Dialogue)) { Press(GameKey.B); Wait(200); }
        while (!Detect(DetectionClass.Dialogue)) { Wait(200); }
        while (Detect(DetectionClass.Dialogue)) { Press(GameKey.B); Wait(200); }
    }

    protected override bool ShinyDetect()
    {
        Press(GameKey.Start); Press(GameKey.A);
        WaitTillBlack();
        WaitTillNotBlack();
        Press(GameKey.A); Wait(200); Press(GameKey.A);
        WaitTillBlack();
        WaitTillNotBlack();
        Wait(500);
        return Detect(DetectionClass.FrLg_s);
    }

    protected override void AfterDetect()
    {
        SoftReset();
    }
}

public class RSEStartersCore : GeneralCore
{
    public RSEStartersCore(IntPtr hwnd, APTask owner, TaskParams config) : base(hwnd, owner, config) { }
    protected override void Encounter()
    {
        Press(GameKey.A);
        WaitTillBlack();
        WaitTillNotBlack();
        if (config.extraData == 0) Press(GameKey.Left);
        else if (config.extraData == 2) Press(GameKey.Right);
        WaitTillBlack(PressA: true);
        WaitTillNotBlack();
    }

    protected override bool ShinyDetect()
    {
        while (!Detect(DetectionClass.Next)) Wait(200);
        Press(GameKey.A);
        while (!Detect(DetectionClass.CanRun)) if (detectRes.Contains(DetectionClass.ShinyStar)) return true;
        return false;
    }

    protected override void AfterDetect()
    {
        SoftReset();
    }
}

public class NormalHitACore : GeneralCore
{
    public NormalHitACore(IntPtr hwnd, APTask owner, TaskParams config) : base(hwnd, owner, config) { }
    protected override void Encounter()
    {
        switch (config.extraData)
        {
            case 0: break;
            case 1: Press(GameKey.Left); break;
            case 2: Press(GameKey.Right); break;
            case 3: Press(GameKey.Up); break;
            case 4: Press(GameKey.Down); break;
        }
        WaitTillBlack(PressA: true);
        WaitTillNotBlack();
    }

    protected override bool ShinyDetect()
    {
        while (!Detect(DetectionClass.Next)) if (detectRes.Contains(DetectionClass.ShinyStar)) return true;
        return false;

    }

    protected override void AfterDetect()
    {
        SoftReset();
    }
}


