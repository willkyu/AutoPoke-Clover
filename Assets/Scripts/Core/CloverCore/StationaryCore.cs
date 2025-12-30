using System;
using Unity.VisualScripting;
using UnityEngine;

public class FrLgStartersCore : GeneralCore
{
    public FrLgStartersCore(IntPtr hwnd, APTask owner, TaskParams config) : base(hwnd, owner, config) { }

    protected override void Encounter()
    {
        Debug.Log("start");
        while (!Detect(DetectionClass.Options)) { Press(GameKey.A); Wait(1000); } // Make sure the whole text appears.
        // Press(GameKey.A); Wait(1000);
        while (Detect(DetectionClass.Options)) { Press(GameKey.A); }
        while (DetectDialogue()) Press(GameKey.B);
        Debug.Log("no dialogue");
        while (!DetectDialogue()) Wait(500);
        Debug.Log("last dialogue");
        while (DetectDialogue()) Press(GameKey.B);
        Debug.Log("end");

        // bool confirmFlag = true;
        // Press(GameKey.A); Wait(1000);
        // while (DetectDialogue())
        // {
        //     if (detectRes.Contains(DetectionClass.Options) && confirmFlag) { Press(GameKey.A); confirmFlag = false; }
        //     else Press(GameKey.B);
        //     if (confirmFlag) Wait(1000);
        // }
        // while (!DetectDialogue()) Wait(500);
        // while (DetectDialogue()) Press(GameKey.A);
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
        WaitTillBlack(pressA: true);
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
        WaitTillBlack(pressA: true);
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
        Press(GameKey.A); Wait(1000);
        while (DetectDialogue())
        {
            if (detectRes.Contains(DetectionClass.Options) && confirmFlag) { Press(GameKey.A); confirmFlag = false; }
            else Press(GameKey.B);
            if (confirmFlag) Wait(1000);
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

public class MewCore : GeneralCore
{
    public MewCore(IntPtr hwnd, APTask owner, TaskParams config) : base(hwnd, owner, config) { }
    protected override void Encounter()
    {
        // ctrl.KeyDown(GameKey.B);
        // ctrl.KeyDown(GameKey.B, rand.Next(0, 10));
        while (!DetectBlack()) Press(GameKey.Up);
        WaitTillNotBlack();
        Wait(3000);
        for (int i = 0; i < 5; i++) Press(GameKey.Up);
        for (int i = 0; i < 5; i++) Press(GameKey.Right);
        for (int i = 0; i < 8; i++) Press(GameKey.Up);
        Press(GameKey.Right);
        while (!DetectBlack()) Press(GameKey.A);
        WaitTillNotBlack();
    }

    protected override bool ShinyDetect()
    {
        return ShinyDetectInBattle();
    }

    protected override void AfterDetect()
    {
        Run();
        Wait(800);
        while (DetectDialogue()) Press(GameKey.B);
        for (int i = 0; i < 6; i++) Press(GameKey.Left);
        ctrl.KeyDown(GameKey.B);
        while (!DetectBlack()) Press(GameKey.Down, wait: false);
        ctrl.KeyUp(GameKey.B);
        Wait(1200);
        Press(GameKey.Right); Press(GameKey.Right);
    }
}