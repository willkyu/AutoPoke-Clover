using System;
using UnityEngine;

public class MoveCore : GeneralCore
{
    public MoveCore(IntPtr hwnd, APTask owner, TaskParams config) : base(hwnd, owner, config) { }
    protected override void Encounter()
    {
        if (config.sweetScent) SweetScentEncounter();
        else NormalEncounter();
    }

    private void NormalEncounter()
    {
        Debug.Log("start");
        ReleaseAllKeys();
        if (config.repel) UseRepel();
        if (!config.jump) while (!DetectBlack())
            {
                while (Detect(DetectionClass.Dialogue)) { Press(GameKey.B); Wait(300); repelFlag = false; }
                if (!repelFlag && config.repel) UseRepel();
                if (config.run) ctrl.KeyDown(GameKey.B);
                RandomPress(config.ifLR ? LeftRightKeys : UpDownKeys);
            }
        else while (!DetectBlack()) { ctrl.KeyDown(GameKey.B, config.counter % 2); Wait(200); }
        if (config.run || config.jump) ctrl.KeyUp(GameKey.B);
        ReleaseAllKeys();
        WaitTillNotBlack();
    }
    private void SweetScentEncounter()
    {
        Press(GameKey.Start); Press(GameKey.A);
        WaitTillBlack();
        WaitTillNotBlack();
        Press(GameKey.Up); Press(GameKey.Up);
        if (config.ifLR) { Press(GameKey.A); Press(GameKey.Down); }
        WaitTillBlack(PressA: true);
        WaitTillNotBlack();
    }

    protected override bool ShinyDetect()
    {
        while (!Detect(DetectionClass.Next)) if (detectRes.Contains(DetectionClass.ShinyStar)) return true;
        Press(GameKey.A);
        Press(GameKey.A);
        while (!Detect(DetectionClass.CanRun)) { Press(GameKey.B); Wait(200); }
        return false;
    }

    protected override void AfterDetect()
    {
        Run();
    }
}