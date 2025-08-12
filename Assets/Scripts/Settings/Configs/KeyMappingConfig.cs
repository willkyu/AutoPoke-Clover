using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class KeyMappingConfig
{
    // 这些字段会被 JSON 序列化保存
    public string a = "X";
    public string b = "Z";
    public string up = "UpArrow";
    public string down = "DownArrow";
    public string left = "LeftArrow";
    public string right = "RightArrow";
    public string start = "Return";
    public string select = "Backspace";

    // 对应属性（大写），自动转换为 KeyCode（运行时使用）
    public KeyCode A => ToKeyCode(a);
    public KeyCode B => ToKeyCode(b);
    public KeyCode Up => ToKeyCode(up);
    public KeyCode Down => ToKeyCode(down);
    public KeyCode Left => ToKeyCode(left);
    public KeyCode Right => ToKeyCode(right);
    public KeyCode Start => ToKeyCode(start);
    public KeyCode Select => ToKeyCode(select);

    private KeyCode ToKeyCode(string keyStr)
    {
        if (Enum.TryParse<KeyCode>(keyStr, true, out var keyCode))
            return keyCode;
        Debug.LogWarning($"⚠️ 无法解析 KeyCode: {keyStr}");
        return KeyCode.None;
    }

    // ✅ 快捷映射字段（字符串访问用）
    private static readonly HashSet<string> ValidKeys = new()
    {
        "a", "b", "up", "down", "left", "right", "start", "select"
    };

    // 用于 UI 修改时访问
    public string Get(string key)
    {
        return key.ToLower() switch
        {
            "a" => a,
            "b" => b,
            "up" => up,
            "down" => down,
            "left" => left,
            "right" => right,
            "start" => start,
            "select" => select,
            _ => throw new ArgumentException($"❌ 无效的按键字段名: {key}")
        };
    }

    public void Set(string key, string value)
    {
        switch (key.ToLower())
        {
            case "a": a = value; break;
            case "b": b = value; break;
            case "up": up = value; break;
            case "down": down = value; break;
            case "left": left = value; break;
            case "right": right = value; break;
            case "start": start = value; break;
            case "select": select = value; break;
            default:
                throw new ArgumentException($"❌ 无效的按键字段名: {key}");
        }
    }

}
