using System;
using UnityEngine;

public class FishCore : GeneralCore
{
    public FishCore(IntPtr hwnd, APTask owner, TaskParams config) : base(hwnd, owner, config) { }

    protected override void Encounter()
    {
        if (config.gameVersion == GameVersion.FrLg)
            FrLgEncounter();
        else RSEEncounter();
    }

    private void FrLgEncounter()
    {
        while (!DetectBlack())
        {
            Press(GameKey.Select);
            while (Detect(DetectionClass.Dialogue)) Press(GameKey.A); Wait(200);
        }
        WaitTillNotBlack();
    }

    private void RSEEncounter()
    {
        while (true)
        {
            Press(GameKey.Select);
            while (true)
            {
                if (Detect(DetectionClass.BlankDialogue)) { Wait(200); continue; }
                if (detectRes.Contains(DetectionClass.Dialogue)) { Press(GameKey.A); continue; }
                if (DetectBlack()) { WaitTillNotBlack(); return; }
                Press(GameKey.Select);
            }
        }


        // throw new System.NotImplementedException();
        // bool getFlag = false;
        // while (true)
        // {
        //     getFlag = true;
        //     Press(GameKey.Select);
        //     while (!Detect(config.language == Language.Eng ? DetectionClass.GetFishEng : DetectionClass.GetFishJpn))
        //     {
        //         if (detectRes.Contains(config.language == Language.Eng ? DetectionClass.FishGoneEng : DetectionClass.FishGoneJpn)) { Debug.Log("fishgone"); getFlag = false; break; }
        //         if (detectRes.Contains(config.language == Language.Eng ? DetectionClass.NoFishEng : DetectionClass.NoFishJpn)) { Debug.Log("nofish"); getFlag = false; break; }
        //         if (detectRes.Contains(config.language == Language.Eng ? DetectionClass.BiteEng : DetectionClass.BiteJpn)) { Debug.Log("bite"); Press(GameKey.A); }
        //     }
        //     Press(GameKey.A);
        //     if (getFlag) break;
        // }
        // WaitTillBlack();
        // WaitTillNotBlack();

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