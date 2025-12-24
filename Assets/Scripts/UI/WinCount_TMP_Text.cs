using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class WinCount_TMP_Text : MonoBehaviour
{
    private TMP_Text label;


    void Awake()
    {
        label = GetComponent<TMP_Text>();
        EventManager.I.AddListener(EventName.SetWinCount, UpdateWinCount);
    }

    void OnDestroy()
    {
        EventManager.I.RemoveListener(EventName.SetWinCount, UpdateWinCount);

    }

    void UpdateWinCount(object sender, EventArgs args)
    {
        var val = args as SetWinCountEventArgs;
        MainThreadDispatcher.Enqueue(() =>
            {
                label.text = $"{val.winCount}";
            });
    }
}
