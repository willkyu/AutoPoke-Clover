using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class ControlUtilsFactory
{
    static public ControlUtils GenerateControlUtils(IntPtr hwnd, TaskParams p)
    {
        if (p.ifNS)
            return new ControlUtilsNS(p.speed);
        return new ControlUtilsKB(hwnd, p.speed);

    }
}

public interface ControlUtils
{
    public void KeyDown(GameKey gameKey, int lParam = 0);
    public void KeyUp(GameKey gameKey);
    public void KeyHit(GameKey gameKey, int hitDuration = 100);
    public void KeysHit(GameKey[] gameKeys, int hitDuration = 100);
}

public class ControlUtilsKB : ControlUtils
{
    IntPtr hwnd;
    float speed;
    public ControlUtilsKB(IntPtr hwnd, float spd = 1f)
    {
        this.hwnd = hwnd;
        speed = spd;
    }
    public void KeyDown(GameKey gameKey, int lParam = 0)
    {
        // Debug.Log($"keydown {gameKey.ToString()}");
        Win32Utils.PressKey(hwnd, Settings.Keys.GetKey(gameKey), lParam);
    }
    public void KeyUp(GameKey gameKey)
    {
        // Debug.Log($"keyup {gameKey.ToString()}");
        Win32Utils.ReleaseKey(hwnd, Settings.Keys.GetKey(gameKey));
    }
    public void KeyHit(GameKey gameKey, int hitDuration = 100)
    {
        // CoroutineRunner.RunSafe(KeyHitCO(gameKey, hitDuration / speed));
        // KeyHitCO(gameKey, hitDuration / speed);
        KeyDown(gameKey);
        Thread.Sleep((int)(hitDuration / speed));
        KeyUp(gameKey);
    }
    // private IEnumerator KeyHitCO(GameKey gameKey, float hitDuration)
    // {
    //     KeyDown(gameKey);
    //     yield return new WaitForSecondsRealtime(hitDuration);
    //     KeyUp(gameKey);
    // }

    public void KeysHit(GameKey[] gameKeys, int hitDuration = 100)
    {
        // CoroutineRunner.RunSafe(KeysHitCO(gameKeys, hitDuration / speed));
        // KeysHitCO(gameKeys, hitDuration / speed);
        foreach (GameKey key in gameKeys)
        {
            KeyDown(key);
        }
        Thread.Sleep((int)(hitDuration / speed));
        foreach (GameKey key in gameKeys)
        {
            KeyUp(key);
        }
    }
    // private IEnumerator KeysHitCO(GameKey[] gameKeys, float hitDuration)
    // {
    //     foreach (GameKey key in gameKeys)
    //     {
    //         KeyDown(key);
    //     }
    //     yield return new WaitForSecondsRealtime(hitDuration);
    //     foreach (GameKey key in gameKeys)
    //     {
    //         KeyUp(key);
    //     }
    // }
}

public class ControlUtilsNS : ControlUtils
{
    private static readonly EasyCon.NsButton[] GameKeyToNsButton =
    {
        EasyCon.NsButton.A,
        EasyCon.NsButton.B,
        EasyCon.NsButton.Up,
        EasyCon.NsButton.Down,
        EasyCon.NsButton.Left,
        EasyCon.NsButton.Right,
        EasyCon.NsButton.Plus,
        EasyCon.NsButton.Minus,
        EasyCon.NsButton.L,
        EasyCon.NsButton.R
    };

    EasyCon easyCon;
    float speed;
    public ControlUtilsNS(float spd = 1f)
    {
        easyCon = EasyCon.Instance;
        speed = spd;
    }
    public void KeyDown(GameKey gameKey, int lParam = 0)
    {
        var keyIndex = (int)gameKey;
        if ((uint)keyIndex >= (uint)GameKeyToNsButton.Length)
        {
            return;
        }

        easyCon.SendButtonDown(GameKeyToNsButton[keyIndex]);
    }
    public void KeyUp(GameKey gameKey)
    {
        var keyIndex = (int)gameKey;
        if ((uint)keyIndex >= (uint)GameKeyToNsButton.Length)
        {
            return;
        }

        easyCon.SendButtonUp(GameKeyToNsButton[keyIndex]);
    }
    public void KeyHit(GameKey gameKey, int hitDuration = 100)
    {
        KeyDown(gameKey);
        Thread.Sleep((int)(hitDuration / speed));
        KeyUp(gameKey);
    }


    public void KeysHit(GameKey[] gameKeys, int hitDuration = 100)
    {
        foreach (GameKey key in gameKeys)
        {
            KeyDown(key);
        }
        Thread.Sleep((int)(hitDuration / speed));
        foreach (GameKey key in gameKeys)
        {
            KeyUp(key);
        }
    }
    public void Capture()
    {
        easyCon.SendButtonDown(EasyCon.NsButton.Capture);
        Thread.Sleep(100);
        easyCon.SendButtonUp(EasyCon.NsButton.Capture);

        Thread.Sleep(1000);

        easyCon.SendButtonDown(EasyCon.NsButton.Capture);
        Thread.Sleep(3000);
        easyCon.SendButtonUp(EasyCon.NsButton.Capture);
    }

}
