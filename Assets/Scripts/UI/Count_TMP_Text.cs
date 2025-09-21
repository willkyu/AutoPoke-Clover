using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class Count_TMP_Text : MonoBehaviour
{
    private TMP_Text label;


    void Awake()
    {
        label = GetComponent<TMP_Text>();
        EventManager.I.AddListener(EventName.SetCounter, UpdateCount);
    }

    void OnDestroy()
    {
        EventManager.I.RemoveListener(EventName.SetCounter, UpdateCount);

    }

    void UpdateCount(object sender, EventArgs args)
    {
        var val = args as SetCounterEventArgs;
        MainThreadDispatcher.Enqueue(() =>
            {
                label.text = $"Count: {val.count}";
            });
    }
}
