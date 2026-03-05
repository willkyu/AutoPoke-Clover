using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class FPS_TMP_Text : MonoBehaviour
{
    private TMP_Text label;


    void Awake()
    {
        label = GetComponent<TMP_Text>();
        EventManager.I.AddListener(EventName.SetFPS, UpdateFPS);
    }

    void OnDestroy()
    {
        EventManager.I.RemoveListener(EventName.SetFPS, UpdateFPS);

    }

    void UpdateFPS(object sender, EventArgs args)
    {
        var val = args as SetFPSEventArgs;
        MainThreadDispatcher.Enqueue(() =>
            {
                label.text = $"FPS: {val.fps:F1}";
                if (val.fps < 12) label.color = Color.red;
                else if (val.fps < 30) label.color = Color.yellow;
                else label.color = Color.green;
            });
    }
}
