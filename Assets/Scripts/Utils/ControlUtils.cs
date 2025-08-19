using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class ControlUtils
{
    IntPtr hWnd;
    float speed;
    public ControlUtils(IntPtr hwnd, float spd = 1f)
    {
        hWnd = hwnd;
        speed = spd;
    }
    public void KeyDown(GameKey gameKey)
    {
        Win32Utils.PressKey(hWnd, Settings.Keys.GetKey(gameKey));
    }
    public void KeyUp(GameKey gameKey)
    {
        Win32Utils.ReleaseKey(hWnd, Settings.Keys.GetKey(gameKey));
    }
    public void KeyHit(GameKey gameKey, float hitDuration = 0.1f)
    {
        CoroutineRunner.RunSafe(KeyHitCO(gameKey, hitDuration / speed));
    }
    private IEnumerator KeyHitCO(GameKey gameKey, float hitDuration)
    {
        KeyDown(gameKey);
        yield return new WaitForSecondsRealtime(hitDuration);
        KeyUp(gameKey);
    }
}
