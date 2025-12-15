using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

[RequireComponent(typeof(TMP_Dropdown))]
public class AlphaExtraEnumDropdownPlugin : MonoBehaviour
{
    [Header("Binding Target")]
    public MonoBehaviour target;
    public string fieldName = "extraData";
    public StationaryMode stationaryMode;

    [Header("Dict: StationaryMode -> Options")]
    public Dictionary<StationaryMode, List<string>> extraDict =
        new Dictionary<StationaryMode, List<string>>
        {
            // { StationaryMode.RSEStarters, new List<string> { "Treecko", "Torchic", "Mudkip" } }
            { StationaryMode.RSEStarters, new List<string> { "木守宫", "火雉鸡", "水跃鱼" } },
            { StationaryMode.NormalHitA, new List<string> { "不执行方向操作", "左", "右", "上", "下" } },
            {StationaryMode.Gift, new List<string> { "需要确认是否获取", "不需要确认是否获取" }},


        };

    private TMP_Dropdown dropdown;
    private FieldInfo field;
    private List<string> values = new List<string>();
    private int idx;

    void Awake()
    {
        dropdown = GetComponent<TMP_Dropdown>();

        Assert.IsNotNull(target, "[AlphaExtraEnumDropdownPlugin] target is null.");
        Assert.IsFalse(string.IsNullOrEmpty(fieldName), "[AlphaExtraEnumDropdownPlugin] fieldName is empty.");


        field = target.GetType().GetField(fieldName);
        Assert.IsNotNull(field, $"[AlphaExtraEnumDropdownPlugin] Field '{fieldName}' not found on {target.GetType().Name}.");
        Assert.IsTrue(field.FieldType == typeof(int), $"[AlphaExtraEnumDropdownPlugin] Field '{fieldName}' must be int.");

        EventManager.I.AddListener(EventName.SetStationaryMode, UpdateSelf);
        EventManager.I.AddListener(EventName.SetRunning, DisableWhenRunning);


        dropdown.onValueChanged.AddListener(OnDropdownChanged);

        // 初次刷新
        idx = (int)field.GetValue(target);
        ApplyModeAndOptions(stationaryMode, keepIndexFromField: true);
    }

    void OnDestroy()
    {
        EventManager.I.RemoveListener(EventName.SetStationaryMode, UpdateSelf);
        EventManager.I.RemoveListener(EventName.SetRunning, DisableWhenRunning);


        if (dropdown != null)
            dropdown.onValueChanged.RemoveListener(OnDropdownChanged);
    }

    private void OnDropdownChanged(int newIndex)
    {
        if (values == null || values.Count == 0) return;

        idx = Mathf.Clamp(newIndex, 0, values.Count - 1);
        field.SetValue(target, idx);      // 写回 int
        dropdown.RefreshShownValue();
    }

    private static List<TMP_Dropdown.OptionData> BuildOptions(List<string> vals)
    {
        var list = new List<TMP_Dropdown.OptionData>(vals.Count);
        for (int i = 0; i < vals.Count; i++)
            list.Add(new TMP_Dropdown.OptionData(vals[i]));
        return list;
    }

    private void UpdateSelf(object sender, EventArgs args)
    {
        if (args is not SetStationaryModeEventArgs e) return;
        stationaryMode = e.stationaryMode;
        ApplyModeAndOptions(stationaryMode, keepIndexFromField: false);
    }

    /// <summary>
    /// 根据模式刷新选项与显隐。
    /// keepIndexFromField=true 表示用目标字段里的 idx；否则重置为 0。
    /// </summary>
    private void ApplyModeAndOptions(StationaryMode mode, bool keepIndexFromField)
    {
        bool hasOptions = extraDict.TryGetValue(mode, out var opts) && opts != null && opts.Count > 0;

        dropdown.gameObject.SetActive(hasOptions);

        if (!hasOptions)
            return;

        values = opts;

        dropdown.ClearOptions();
        dropdown.AddOptions(BuildOptions(values));

        idx = keepIndexFromField ? Mathf.Clamp((int)field.GetValue(target), 0, values.Count - 1) : 0;
        field.SetValue(target, idx);
        dropdown.SetValueWithoutNotify(idx);
        dropdown.RefreshShownValue();
    }

    void DisableWhenRunning(object sender, EventArgs args)
    {
        var val = args as SetRunningEventArgs;
        dropdown.interactable = !val.running;
    }
}
