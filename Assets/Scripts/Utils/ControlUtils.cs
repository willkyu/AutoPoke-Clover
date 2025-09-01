using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class ControlUtils
{
    IntPtr hwnd;
    float speed;
    public ControlUtils(IntPtr hwnd, float spd = 1f)
    {
        this.hwnd = hwnd;
        speed = spd;
    }
    public void KeyDown(GameKey gameKey)
    {
        // Debug.Log($"keydown {gameKey.ToString()}");
        Win32Utils.PressKey(hwnd, Settings.Keys.GetKey(gameKey));
    }
    public void KeyUp(GameKey gameKey)
    {
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
