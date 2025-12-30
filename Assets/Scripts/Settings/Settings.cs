using System;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;

[Serializable]
public class Configs
{
    public GeneralConfig general = new GeneralConfig();
    public KeyMappingConfig keyMapping = new KeyMappingConfig();
    public NotificationConfig notification = new NotificationConfig();
    public int totalCount = 0;
}

public class Settings
{
    private static Configs _configs;
    private static string settingsPath = Path.Combine(Application.persistentDataPath, "AutoPoke.settings.json");

    public static Configs Current
    {
        get
        {
            if (_configs == null)
                LoadSettings();
            return _configs;
        }
    }

    public static GeneralConfig General => Current.general;
    public static KeyMappingConfig Keys => Current.keyMapping;
    public static NotificationConfig Notification => Current.notification;

    public static void LoadSettings()
    {
        if (File.Exists(settingsPath))
        {
            try
            {
                string json = File.ReadAllText(settingsPath);
                _configs = JsonUtility.FromJson<Configs>(json);
                Debug.Log("✅ Settings loaded successfully.");
            }
            catch
            {
                Debug.LogWarning("⚠️ Failed to parse settings, using default.");
                _configs = new Configs();
                SaveSettings();
            }
        }
        else
        {
            _configs = new Configs();
            SaveSettings();
        }
    }

    public static void SaveSettings()
    {
        string json = JsonUtility.ToJson(_configs, true);
        File.WriteAllText(settingsPath, json);
        Debug.Log("💾 Settings saved to " + settingsPath);
    }

    public static void ResetKeyMapping()
    {
        Current.keyMapping = new KeyMappingConfig();
        SaveSettings();
        Current.TriggerEvent(EventName.Refresh);
    }

}
