using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class StartBtnPlugin : MonoBehaviour
{
    public APTask currentTask;
    private bool running = false;
    private TMP_Text label;
    private Button btn;

    private void Awake()
    {
        label = GetComponentInChildren<TMP_Text>();
        btn = GetComponent<Button>();

        Assert.IsNotNull(label, "No TMP_Text in children.");
        Assert.IsNotNull(currentTask, "No Task.");

        running = currentTask.IsRunning;
        UpdateSelf();

        EventManager.I.AddListener(EventName.SetRunning, UpdateWhenRunningSet);

    }

    void OnDestroy()
    {
        EventManager.I.RemoveListener(EventName.SetRunning, UpdateWhenRunningSet);
    }

    private void UpdateWhenRunningSet(object sender, EventArgs args)
    {
        var val = args as SetRunningEventArgs;
        running = val.running;
        UpdateSelf();
    }

    private void UpdateSelf()
    {
        label.text = running ? "Stop" : "Start";
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(running ? currentTask.StopTask : currentTask.StartTask);
    }
}
