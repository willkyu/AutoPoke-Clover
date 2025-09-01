using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GeneralDropdown : MonoBehaviour
{

    public Text label;  // 在 Inspector 中指定
    public Dropdown dropdown;

    public void OnDropdownChanged(int index)
    {
        string selected = dropdown.options[index].text;
        // label.text = $"你选中了：{selected}";
        Debug.Log($"selected: {selected} label: {label.text}");

    }
}
