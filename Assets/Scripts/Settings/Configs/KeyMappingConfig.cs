using System;
using System.Collections.Generic;
using UnityEngine;

public enum GameKey
{
    A,
    B,
    Up,
    Down,
    Left,
    Right,
    Start,
    Select,
    L,
    R
}


[Serializable]
public class KeyMappingConfig
{
    // 仍然用字符串保存（方便 JSON 序列化）
    public string a = "X";
    public string b = "Z";
    public string up = "UpArrow";
    public string down = "DownArrow";
    public string left = "LeftArrow";
    public string right = "RightArrow";
    public string start = "Return";
    public string select = "Backspace";
    public string l = "A";
    public string r = "S";

    // 转换为 KeyCode
    public KeyCode A => ToKeyCode(a);
    public KeyCode B => ToKeyCode(b);
    public KeyCode Up => ToKeyCode(up);
    public KeyCode Down => ToKeyCode(down);
    public KeyCode Left => ToKeyCode(left);
    public KeyCode Right => ToKeyCode(right);
    public KeyCode Start => ToKeyCode(start);
    public KeyCode Select => ToKeyCode(select);
    public KeyCode L => ToKeyCode(l);
    public KeyCode R => ToKeyCode(r);

    private KeyCode ToKeyCode(string keyStr)
    {
        if (Enum.TryParse<KeyCode>(keyStr, true, out var keyCode))
            return keyCode;
        Debug.LogWarning($"⚠️ 无法解析 KeyCode: {keyStr}");
        return KeyCode.None;
    }

    // ✅ 用枚举来访问，不再需要字符串 switch
    public string Get(GameKey key) => key switch
    {
        GameKey.A => a,
        GameKey.B => b,
        GameKey.Up => up,
        GameKey.Down => down,
        GameKey.Left => left,
        GameKey.Right => right,
        GameKey.Start => start,
        GameKey.Select => select,
        GameKey.L => l,
        GameKey.R => r,
        _ => throw new ArgumentOutOfRangeException(nameof(key), key, null)
    };

    public void Set(GameKey key, string value)
    {
        switch (key)
        {
            case GameKey.A: a = value; break;
            case GameKey.B: b = value; break;
            case GameKey.Up: up = value; break;
            case GameKey.Down: down = value; break;
            case GameKey.Left: left = value; break;
            case GameKey.Right: right = value; break;
            case GameKey.Start: start = value; break;
            case GameKey.Select: select = value; break;
            case GameKey.L: l = value; break;
            case GameKey.R: r = value; break;
            default: throw new ArgumentOutOfRangeException(nameof(key), key, null);
        }
    }

    // ✅ 用枚举来访问，不再需要字符串 switch
    public KeyCode GetKey(GameKey key) => key switch
    {
        GameKey.A => A,
        GameKey.B => B,
        GameKey.Up => Up,
        GameKey.Down => Down,
        GameKey.Left => Left,
        GameKey.Right => Right,
        GameKey.Start => Start,
        GameKey.Select => Select,
        GameKey.L => L,
        GameKey.R => R,
        _ => throw new ArgumentOutOfRangeException(nameof(key), key, null)
    };
}
