using System;
using Unity.VisualScripting;
using UnityEngine;

public class FrLgStartersCore : GeneralCore
{
    public FrLgStartersCore(IntPtr hwnd, APTask owner, TaskParams config) : base(hwnd, owner, config) { }

    protected override void Encounter()
    {
        bool confirmFlag = true;
        Press(GameKey.A); Wait(800);
        while (DetectDialogue())
        {
            if (detectRes.Contains(DetectionClass.Options) && confirmFlag) { Press(GameKey.A); confirmFlag = false; }
            else Press(GameKey.B);
            if (confirmFlag) Wait(800);
        }
        while (!DetectDialogue()) Wait(500);
        while (DetectDialogue()) Press(GameKey.A);
    }

    protected override bool ShinyDetect()
    {
        return ShinyDetectInBag(partyIdx: 0, checkFirst: true);
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
        return ShinyDetectInBattle(checkEnemy: false);
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
        return ShinyDetectInBattle(checkEnemy: true, SL: true);
    }

    protected override void AfterDetect()
    {
        SoftReset();
    }
}

public class GiftCore : GeneralCore
{
    public GiftCore(IntPtr hwnd, APTask owner, TaskParams config) : base(hwnd, owner, config) { }
    protected override void Encounter()
    {
        bool confirmFlag = config.extraData == 0;
        Press(GameKey.A); Wait(800);
        while (DetectDialogue())
        {
            if (detectRes.Contains(DetectionClass.Options) && confirmFlag) { Press(GameKey.A); confirmFlag = false; }
            else Press(GameKey.B);
            if (confirmFlag) Wait(800);
        }
    }

    protected override bool ShinyDetect()
    {
        return ShinyDetectInBag();
    }

    protected override void AfterDetect()
    {
        SoftReset();
    }
}
