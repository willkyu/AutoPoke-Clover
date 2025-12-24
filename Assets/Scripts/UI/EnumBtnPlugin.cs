using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

// [RequireComponent(typeof(Button))]
// public class EnumBtnPlugin : MonoBehaviour
// {

//     public Sprite[] sprites;
//     public MonoBehaviour target;
//     public string fieldName;
//     private int idx;
//     private Button btn;
//     private FieldInfo field;
//     private Array values;
//     void Awake()
//     {
//         btn = GetComponent<Button>();
//         // 初始化 UI
//         field = target.GetType().GetField(fieldName);
//         if (field != null && field.FieldType.IsEnum)
//         {
//             idx = (int)field.GetValue(target);
//             Assert.IsTrue(idx >= 0 && idx < sprites.Length, "Idx is outside the bounds of sprites array.");
//             btn.image.sprite = sprites[idx];
//             btn.onClick.AddListener(OnClick);
//             // 获取所有枚举值
//             values = Enum.GetValues(field.FieldType);
//             Assert.AreEqual(sprites.Length, values.Length, "Lengths of sprites and enums are not equal.");
//         }

//     }

//     void OnClick()
//     {
//         idx = (idx + 1) % values.Length;
//         field.SetValue(target, values.GetValue(idx));
//         btn.image.sprite = sprites[idx];
//     }

// }



[RequireComponent(typeof(Button))]
public class AlphaEnumBtnPlugin : MonoBehaviour
{

    // public Sprite[] sprites;
    public APTask target;
    public string fieldName;
    private int idx;
    private Button btn;
    private TMP_Text label;
    private FieldInfo field;
    private Array values;
    void Awake()
    {
        btn = GetComponent<Button>();
        label = GetComponentInChildren<TMP_Text>();
        Assert.IsNotNull(label, "[AlphaEnumBtnPlugin] No TMP_Text in children.");
        // 初始化 UI
        field = target.GetType().GetField(fieldName);
        if (field != null && field.FieldType.IsEnum)
        {
            idx = (int)field.GetValue(target);
            btn.onClick.AddListener(OnClick);
            // 获取所有枚举值
            values = Enum.GetValues(field.FieldType);
            label.text = values.GetValue(idx).ToString();
        }

        EventManager.I.AddListener(EventName.SetRunning, DisableWhenRunning);

    }

    void OnDestroy()
    {
        EventManager.I.RemoveListener(EventName.SetRunning, DisableWhenRunning);

        if (btn != null)
            btn.onClick.RemoveListener(OnClick);
    }

    void OnClick()
    {
        idx = (idx + 1) % values.Length;
        field.SetValue(target, values.GetValue(idx));
        label.text = values.GetValue(idx).ToString();
        if (fieldName == "function") this.TriggerEvent(EventName.SetFunction, new SetFunctionEventArgs { function = (Function)idx });
    }

    void DisableWhenRunning(object sender, EventArgs args)
    {
        var val = args as SetRunningEventArgs;
        btn.interactable = !val.running;
        Debug.Log($"Interactable switch to {val.running}");
    }

}
