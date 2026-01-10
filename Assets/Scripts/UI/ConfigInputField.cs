using TMPro;
using UnityEngine;

public class ConfigInputField : MonoBehaviour
{
    public enum ConfigScope
    {
        General,
        Notification,
        Obs
    }

    [Header("Binding")]
    [SerializeField] private ConfigScope scope = ConfigScope.General;
    [SerializeField] private string fieldName = "windowName";

    [Header("UI")]
    [SerializeField] private TMP_InputField input;

    [Header("Options")]
    [SerializeField] private bool saveOnEndEdit = true;
    [SerializeField] private bool saveOnValueChanged = false;
    [SerializeField] private bool trimWhitespace = true;

    private bool _suppressCallback;

    void Reset()
    {
        input = GetComponentInChildren<TMP_InputField>();
    }

    void Awake()
    {
        if (input == null)
        {
            Debug.LogError($"{name}: TMP_InputField is not assigned.");
            enabled = false;
            return;
        }

        if (saveOnEndEdit)
            input.onEndEdit.AddListener(OnEndEdit);

        if (saveOnValueChanged)
            input.onValueChanged.AddListener(OnValueChanged);
    }

    void OnEnable()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        _suppressCallback = true;
        input.text = GetValue();
        _suppressCallback = false;
    }

    private void OnValueChanged(string value)
    {
        if (_suppressCallback) return;
        Apply(value);
    }

    private void OnEndEdit(string value)
    {
        if (_suppressCallback) return;
        Apply(value);
    }

    private void Apply(string value)
    {
        if (trimWhitespace && value != null)
            value = value.Trim();

        SetValue(value);
        Settings.SaveSettings();
    }

    private string GetValue()
    {
        try
        {
            return scope switch
            {
                ConfigScope.General => Settings.General.Get(fieldName),
                ConfigScope.Notification => Settings.Notification.Get(fieldName),
                ConfigScope.Obs => Settings.Obs.Get(fieldName),

                _ => ""
            };
        }
        catch (System.Exception e)
        {
            Debug.LogError($"{name}: Get failed (scope={scope}, field={fieldName}) -> {e.Message}");
            return "";
        }
    }

    private void SetValue(string value)
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
                case ConfigScope.Obs:
                    Settings.Obs.Set(fieldName, value);
                    break;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"{name}: Set failed (scope={scope}, field={fieldName}, value={value}) -> {e.Message}");
        }
    }
}
