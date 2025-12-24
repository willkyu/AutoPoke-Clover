using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;

public class MainViewSwitch : MonoBehaviour
{
    public GameObject[] views;
    public APTask target;
    public string fieldName = "function";
    private int idx;
    private FieldInfo field;
    private Array values;

    void Awake()
    {
        // 初始化 UI
        field = target.GetType().GetField(fieldName);
        if (field != null && field.FieldType.IsEnum)
        {
            idx = (int)field.GetValue(target);
            Assert.IsTrue(idx >= 0 && idx < views.Length, "Idx is outside the bounds of sprites array.");
            // 获取所有枚举值
            values = Enum.GetValues(field.FieldType);
            Assert.AreEqual(views.Length, values.Length, "Lengths of sprites and enums are not equal.");
            for (int i = 0; i < views.Length; i++)
            {
                views[i].SetActive(i == idx);
            }
            EventManager.I.AddListener(EventName.SetFunction, SetView);
        }

    }

    private void OnDestroy()
    {
        EventManager.I.RemoveListener(EventName.SetFunction, SetView);
    }

    void SetView(object sender, EventArgs args)
    {
        var data = args as SetFunctionEventArgs;
        idx = (int)data.function;
        for (int i = 0; i < views.Length; i++)
        {
            views[i].SetActive(i == idx);
        }
    }

}
