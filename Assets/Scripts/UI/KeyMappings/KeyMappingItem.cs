using System;
using UnityEngine;
using UnityEngine.UI;

public class KeyMappingItem : MonoBehaviour
{
    public Text labelText;      // 显示 "Up", "Down" 等
    public Button keyButton;    // 显示当前按键
    private string keyName;     // 当前是 up/down/a 等字段名
    private bool listening = false;

    void Start()
    {
        this.keyName = labelText.text.ToLower();
        Debug.Log(this.keyName);
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
                // ✅ 用新的 Set 方法代替反射
                try
                {
                    Settings.Keys.Set(keyName, code.ToString());
                    Settings.SaveSettings();
                    listening = false;
                    UpdateDisplay();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"⚠️ 设置按键失败: {keyName} → {code}. 原因: {ex.Message}");
                }
                break;
            }
        }
    }

    void UpdateDisplay()
    {
        try
        {
            // ✅ 用新的 Get 方法代替反射
            string keyStr = Settings.Keys.Get(keyName);
            keyButton.GetComponentInChildren<Text>().text = keyStr;
            // Debug.Log(Settings.Keys.A);
        }
        catch (Exception ex)
        {
            // keyButton.GetComponentInChildren<Text>().text = "Invalid";
            Debug.LogWarning($"⚠️ 获取按键失败: {keyName}. 原因: {ex.Message}");
        }
    }
}
