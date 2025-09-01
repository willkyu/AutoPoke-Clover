using System;
using Unity.VisualScripting;

public class FrLgStartersCore : GeneralCore
{
    public FrLgStartersCore(IntPtr hwnd, APTask owner, TaskParams config) : base(hwnd, owner, config) { }

    protected override void Encounter()
    {
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
        Press(GameKey.Start); Press(GameKey.A);
        WaitTillBlack();
        WaitTillNotBlack();
        if (config.extraData == 0) Press(GameKey.Left);
        else if (config.extraData == 2) Press(GameKey.Right);
        Press(GameKey.A); Press(GameKey.A);
        WaitTillBlack();
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

public class FrLgMewtwoCore : GeneralCore
{
    public FrLgMewtwoCore(IntPtr hwnd, APTask owner, TaskParams config) : base(hwnd, owner, config) { }
    protected override void Encounter()
    {
        base.Encounter();
    }

    protected override bool ShinyDetect()
    {
        return base.ShinyDetect();
    }

    protected override void AfterDetect()
    {
        base.AfterDetect();
    }
}


