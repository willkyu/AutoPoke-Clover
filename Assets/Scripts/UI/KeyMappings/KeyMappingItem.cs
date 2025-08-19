using System;
using UnityEngine;
using UnityEngine.UI;

public class KeyMappingItem : MonoBehaviour
{
    public Text labelText;      // 显示 "Up", "Down" 等
    public Button keyButton;    // 显示当前按键
    private GameKey key;        // 用枚举替代 string
    private bool listening = false;

    void Start()
    {
        // 把 UI 上的文字转成枚举，比如 "Up" → GameKey.Up
        if (!Enum.TryParse<GameKey>(labelText.text, true, out key))
        {
            Debug.LogWarning($"⚠️ 无法解析 GameKey: {labelText.text}");
            enabled = false; // 禁用这个组件，避免报错
            return;
        }

        UpdateDisplay();
        keyButton.onClick.AddListener(() => StartListening());
    }

    void StartListening()
    {
        listening = true;
        keyButton.GetComponentInChildren<Text>().text = "Press any key...";
    }

    void Update()
    {
        if (!listening) return;

        // 检测任何按键
        foreach (KeyCode code in Enum.GetValues(typeof(KeyCode)))
        {
            if (Input.GetKeyDown(code))
            {
                try
                {
                    Settings.Keys.Set(key, code.ToString());  // ✅ 用枚举
                    Settings.SaveSettings();
                    listening = false;
                    UpdateDisplay();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"⚠️ 设置按键失败: {key} → {code}. 原因: {ex.Message}");
                }
                break;
            }
        }
    }

    void UpdateDisplay()
    {
        try
        {
            string keyStr = Settings.Keys.Get(key); // ✅ 用枚举
            keyButton.GetComponentInChildren<Text>().text = keyStr;
        }
        catch (Exception ex)
        {
            keyButton.GetComponentInChildren<Text>().text = "Invalid";
            Debug.LogWarning($"⚠️ 获取按键失败: {key}. 原因: {ex.Message}");
        }
    }
}
