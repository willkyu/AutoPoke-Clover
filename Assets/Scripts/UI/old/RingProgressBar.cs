using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RingProgressBar : MonoBehaviour
{
    public int max = 8192;

    [SerializeField]
    private int _value = 0;
    public Image ring;
    public Image ringHead;
    public Image ringTail;
    public Image trail;

    public int value
    {
        get => _value;
        set
        {
            _value = value;
            UpdateRing();
        }
    }



    private void UpdateRing()
    {
        if (ring == null || ringHead == null || max == 0) return;

        float percent = (_value % max) / (float)max;
        ring.fillAmount = percent;

        // 计算从顶部开始顺时针旋转的角度
        float angle = -percent * 360f + 90f;

        // 计算偏移位置（自动考虑 ringHead 尺寸）
        float ringRadius = ((RectTransform)ring.rectTransform).rect.width * 0.5f;
        float headOffset = ringHead.rectTransform.rect.width * 0.5f; // 自动适配
        float distance = ringRadius - headOffset;

        Vector2 offset = new Vector2(
            Mathf.Cos(angle * Mathf.Deg2Rad),
            Mathf.Sin(angle * Mathf.Deg2Rad)
        ) * distance;

        ringHead.rectTransform.anchoredPosition = offset;
    }

    private void Start()
    {
        UpdateRing();
    }

    public int testValue = 0;

    private void OnValidate()
    {
        value = testValue;
    }
}
