using System;
using UnityEngine;
using UnityEngine.UI;

public class ConfigToggleRow : MonoBehaviour
{
    public enum ConfigScope
    {
        General,
        Notification
    }

    [Header("Binding")]
    [SerializeField] private ConfigScope scope = ConfigScope.Notification;
    [SerializeField] private string fieldName = "sendToast";

    [Header("UI")]
    [SerializeField] private Toggle toggle;

    [Header("Options")]
    [SerializeField] private bool saveImmediately = true;

    private bool _suppressCallback;

    void Reset()
    {
        toggle = GetComponentInChildren<Toggle>();
    }

    void Awake()
    {
        if (toggle == null)
        {
            Debug.LogError($"{name}: Toggle is not assigned.");
            enabled = false;
            return;
        }

        toggle.onValueChanged.AddListener(OnToggleChanged);
    }

    void OnEnable()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        _suppressCallback = true;

        var str = GetValueString();
        if (bool.TryParse(str, out var v))
        {
            toggle.isOn = v;
        }
        else
        {
            Debug.LogError($"{name}: Field '{fieldName}' is not a bool (value='{str}').");
        }

        _suppressCallback = false;
    }

    private void OnToggleChanged(bool value)
    {
        if (_suppressCallback) return;

        SetValueString(value.ToString());

        if (saveImmediately)
            Settings.SaveSettings();
    }

    private string GetValueString()
    {
        try
        {
            return scope switch
            {
                ConfigScope.General => Settings.General.Get(fieldName),
                ConfigScope.Notification => Settings.Notification.Get(fieldName),
                _ => "False"
            };
        }
        catch (Exception e)
        {
            Debug.LogError($"{name}: Get failed (scope={scope}, field={fieldName}) -> {e.Message}");
            return "False";
        }
    }

    private void SetValueString(string value)
    {
        try
        {
            switch (scope)
            {
                case ConfigScope.General:
                    Settings.General.Set(fieldName, value);
                    break;
                case ConfigScope.Notification:
                    Settings.Notification.Set(fieldName, value);
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"{name}: Set failed (scope={scope}, field={fieldName}) -> {e.Message}");
        }
    }
}
