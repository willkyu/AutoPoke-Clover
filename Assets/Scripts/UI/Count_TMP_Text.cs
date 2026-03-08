using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class Count_TMP_Text : MonoBehaviour
{
    private TMP_Text label;
    [SerializeField] private TMP_InputField inputField;
    private bool suppressInputCallback;


    void Awake()
    {
        label = GetComponent<TMP_Text>();
        if (inputField == null)
            inputField = GetComponentInChildren<TMP_InputField>();

        inputField.onEndEdit.AddListener(OnEndEdit);

        ApplyCountToUI(Settings.Current.currentCount);
        EventManager.I.AddListener(EventName.SetCounter, UpdateCount);
        EventManager.I.AddListener(EventName.SetRunning, DisableWhenRunning);
    }


    void OnDestroy()
    {
        EventManager.I.RemoveListener(EventName.SetCounter, UpdateCount);
        EventManager.I.RemoveListener(EventName.SetRunning, DisableWhenRunning);
        if (inputField != null)
            inputField.onEndEdit.RemoveListener(OnEndEdit);

    }

    void UpdateCount(object sender, EventArgs args)
    {
        var val = args as SetCounterEventArgs;
        if (val == null) return;

        MainThreadDispatcher.Enqueue(() =>
            {
                ApplyCountToUI(val.count);
            });
    }

    private void OnEndEdit(string value)
    {
        if (suppressInputCallback) return;

        if (!int.TryParse(value, out int parsed))
            parsed = Settings.Current.currentCount;

        Settings.Current.currentCount = parsed;
        Settings.SaveSettings();
        ApplyCountToUI(parsed);
    }

    private void ApplyCountToUI(int count)
    {
        if (inputField != null)
        {
            suppressInputCallback = true;
            inputField.text = count.ToString();
            suppressInputCallback = false;
        }
        else
        {
            label.text = $"Count: {count}";
        }
    }
    void DisableWhenRunning(object sender, EventArgs args)
    {
        var val = args as SetRunningEventArgs;
        inputField.interactable = !val.running;
        // Debug.Log($"Interactable switch to {val.running}");
    }
}
