using System;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

// [RequireComponent(typeof(Button))]
// [RequireComponent(typeof(Image))]
// public class BoolBtnPlugin : MonoBehaviour
// {
//     public MonoBehaviour target;
//     public string fieldName;

//     [Header("Sprites when value = true")]
//     public Sprite normalSprite_true;
//     public Sprite highlightedSprite_true;
//     public Sprite pressedSprite_true;
//     public Sprite selectedSprite_true;
//     public Sprite disabledSprite_true;

//     [Header("Sprites when value = false")]
//     public Sprite normalSprite_false;
//     public Sprite highlightedSprite_false;
//     public Sprite pressedSprite_false;
//     public Sprite selectedSprite_false;
//     public Sprite disabledSprite_false;

//     private bool value;
//     private Button btn;
//     private Image image;
//     private FieldInfo field;
//     void Awake()
//     {
//         btn = GetComponent<Button>();
//         image = GetComponent<Image>();
//         // 初始化 UI
//         field = target.GetType().GetField(fieldName);
//         if (field != null && field.FieldType == typeof(bool))
//         {
//             value = (bool)field.GetValue(target);
//             Assert.IsTrue(btn.transition == Selectable.Transition.SpriteSwap, "[BoolBtnPlugin] Please set Button.transition as SpriteSwap");
//             value = (bool)field.GetValue(target);
//             btn.onClick.AddListener(OnClick);
//         }
//     }

//     void ApplySprites()
//     {
//         SpriteState ss = btn.spriteState;
//         image.sprite = value ? normalSprite_true : normalSprite_false;
//         ss.highlightedSprite = value ? highlightedSprite_true : highlightedSprite_false;
//         ss.pressedSprite = value ? pressedSprite_true : pressedSprite_false;
//         ss.selectedSprite = value ? selectedSprite_true : selectedSprite_false;
//         ss.disabledSprite = value ? disabledSprite_true : disabledSprite_false;
//     }

//     void OnClick()
//     {
//         value = !value;
//         field.SetValue(target, value);
//         ApplySprites();
//     }

// }

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(Image))]
public class AlphaBoolBtnPlugin : MonoBehaviour
{
    public APTask target;
    public string fieldName;

    [Header("Sprites when value = true")]
    public Sprite normalSprite_true;

    [Header("Sprites when value = false")]
    public Sprite normalSprite_false;

    private bool value;
    private TMP_Text label;
    private Button btn;
    private Image image;
    private FieldInfo field;
    private Color orginalColor;
    void Awake()
    {
        btn = GetComponent<Button>();
        image = GetComponent<Image>();
        label = GetComponentInChildren<TMP_Text>();
        Assert.IsNotNull(label, "[AlphaEnumBtnPlugin] No TMP_Text in children.");
        orginalColor = label.color;

        // 初始化 UI
        field = target.GetType().GetField(fieldName);
        if (field != null && field.FieldType == typeof(bool))
        {
            value = (bool)field.GetValue(target);
            // Assert.IsTrue(btn.transition == Selectable.Transition.SpriteSwap, "[BoolBtnPlugin] Please set Button.transition as SpriteSwap");
            value = (bool)field.GetValue(target);
            btn.onClick.AddListener(OnClick);
            ApplySprites();
        }

        EventManager.I.AddListener(EventName.SetRunning, DisableWhenRunning);
    }

    void OnDestroy()
    {
        EventManager.I.RemoveListener(EventName.SetRunning, DisableWhenRunning);

        if (btn != null)
            btn.onClick.RemoveListener(OnClick);
    }

    void ApplySprites()
    {
        image.sprite = value ? normalSprite_true : normalSprite_false;
        label.color = value ? Color.white : orginalColor;
    }

    void OnClick()
    {
        value = !value;
        field.SetValue(target, value);
        ApplySprites();
    }

    void DisableWhenRunning(object sender, EventArgs args)
    {
        var val = args as SetRunningEventArgs;
        btn.interactable = !val.running;
        // Debug.Log($"Interactable switch to {val.running}");
    }

}
