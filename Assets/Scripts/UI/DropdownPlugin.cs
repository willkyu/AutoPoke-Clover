using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;                       // ★ 用 TMP_Dropdown
using UnityEngine;
using UnityEngine.Assertions;

[RequireComponent(typeof(TMP_Dropdown))]
public class AlphaEnumDropdownPlugin : MonoBehaviour
{
    [Header("Binding Target")]
    public MonoBehaviour target;   // 要绑定的脚本
    public string fieldName;       // 枚举字段名（支持 public 或 [SerializeField] private）

    [Header("Options")]
    private TMP_Dropdown dropdown;
    private FieldInfo field;
    private Array values;          // 该枚举的所有取值
    private int idx;               // 当前索引

    void Awake()
    {
        dropdown = GetComponent<TMP_Dropdown>();

        Assert.IsNotNull(target, "[AlphaEnumDropdownPlugin] target is null.");
        Assert.IsFalse(string.IsNullOrEmpty(fieldName), "[AlphaEnumDropdownPlugin] fieldName is empty.");

        // 支持 public / private [SerializeField]
        field = target.GetType().GetField(fieldName);
        Assert.IsNotNull(field, $"[AlphaEnumDropdownPlugin] Field '{fieldName}' not found on {target.GetType().Name}.");

        idx = (int)field.GetValue(target);


        Assert.IsTrue(field.FieldType.IsEnum, $"[AlphaEnumDropdownPlugin] Field '{fieldName}' is not an enum.");

        // 取所有枚举值 & 生成下拉项
        values = Enum.GetValues(field.FieldType);


        dropdown.options = BuildOptions(values);
        dropdown.onValueChanged.AddListener(OnDropdownChanged);
        EventManager.I.AddListener(EventName.SetRunning, DisableWhenRunning);



        dropdown.SetValueWithoutNotify(idx);
        dropdown.RefreshShownValue();
    }

    void OnDestroy()
    {
        EventManager.I.RemoveListener(EventName.SetRunning, DisableWhenRunning);

        if (dropdown != null)
            dropdown.onValueChanged.RemoveListener(OnDropdownChanged);
    }

    private void OnDropdownChanged(int newIndex)
    {
        idx = Mathf.Clamp(newIndex, 0, values.Length - 1);
        object val = values.GetValue(idx);
        field.SetValue(target, val);             // 写回目标字段
        dropdown.RefreshShownValue();            // 刷新显示（可选）
        if (fieldName == "stationaryMode") this.TriggerEvent(EventName.SetStationaryMode, new SetStationaryModeEventArgs { stationaryMode = (StationaryMode)idx });
    }

    private static List<TMP_Dropdown.OptionData> BuildOptions(Array enumValues)
    {
        var list = new List<TMP_Dropdown.OptionData>(enumValues.Length);
        for (int i = 0; i < enumValues.Length; i++)
            list.Add(new TMP_Dropdown.OptionData(enumValues.GetValue(i).ToString()));
        return list;
    }

    void DisableWhenRunning(object sender, EventArgs args)
    {
        var val = args as SetRunningEventArgs;
        dropdown.interactable = !val.running;
    }

}
