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
        // ReleaseAllKeys();
        if (config.run) ctrl.KeyDown(GameKey.B, config.counter % 2);

        if (!config.jump) while (!DetectBlack())
            {
                while (DetectDialogue())
                {
                    // Wait(300); if (!DetectDialogue()) break;
                    // Press(GameKey.B); Wait(300); repelFlag = false;
                    Wait(1000); Press(GameKey.B); repelFlag = false; callOrRepelDialogueFlag = true;
                }
                if (callOrRepelDialogueFlag && config.run)
                {
                    ctrl.KeyDown(GameKey.B);
                    ctrl.KeyDown(GameKey.B, config.counter % 2);
                    callOrRepelDialogueFlag = false;
                }
                if (config.repel && !repelFlag) UseRepel();
                // if (config.run) ctrl.KeyDown(GameKey.B);
                RandomPress(config.ifLR ? LeftRightKeys : UpDownKeys);
            }
        else while (!DetectBlack()) { ctrl.KeyDown(GameKey.B, rand.Next(0, 10)); Wait(200); }
        // else WaitTillBlack();
        // if (config.run || config.jump) ctrl.KeyUp(GameKey.B);
        ReleaseAllKeys();
        WaitTillNotBlack();
    }
    private void SweetScentEncounter()
    {
        Press(GameKey.Start); Press(GameKey.A);
        WaitTillBlack();
        WaitTillNotBlack();
        Press(GameKey.Up); Press(GameKey.Up);
        if (config.gameVersion == GameVersion.FrLg) { Press(GameKey.A); Press(GameKey.Down); }
        WaitTillBlack(pressA: true);
        WaitTillNotBlack();
    }

    protected override bool ShinyDetect()
    {
        return ShinyDetectInBattle(checkEnemy: true, SL: false);
    }

    protected override void AfterDetect()
    {
        Run();
    }
}