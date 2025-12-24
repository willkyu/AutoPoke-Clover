using UnityEngine;

public class CardSwitcher : MonoBehaviour
{
    [Header("Cards (Panels)")]
    [SerializeField] private GameObject[] cards;

    [Header("Default")]
    [SerializeField] private int defaultIndex = 0;

    [Header("Options")]
    [Tooltip("切换到某个卡片后，是否自动调用该卡片里所有 ICardRefreshable 的 Refresh()")]
    [SerializeField] private bool refreshOnShow = true;

    public int CurrentIndex { get; private set; } = -1;

    void Start()
    {
        if (cards == null || cards.Length == 0)
        {
            Debug.LogError($"{name}: No cards assigned.");
            return;
        }

        defaultIndex = Mathf.Clamp(defaultIndex, 0, cards.Length - 1);
        ShowByIndex(defaultIndex);
    }

    /// <summary>
    /// Inspector 里最常用：按钮 OnClick 传一个 int 参数（卡片下标）
    /// </summary>
    public void ShowByIndex(int index)
    {
        if (cards == null || cards.Length == 0) return;
        if (index < 0 || index >= cards.Length)
        {
            Debug.LogWarning($"{name}: Invalid card index: {index}");
            return;
        }

        // 如果重复点击当前页，可选择直接 return（这里保留刷新行为）
        CurrentIndex = index;

        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] != null)
                cards[i].SetActive(i == index);
        }

        if (refreshOnShow)
            RefreshCard(cards[index]);
    }

    /// <summary>
    /// Inspector 里也好用：按钮直接传对应 panel（不用记 index）
    /// </summary>
    public void Show(GameObject card)
    {
        if (card == null) return;

        int idx = IndexOf(card);
        if (idx < 0)
        {
            Debug.LogWarning($"{name}: Card not found in cards[]: {card.name}");
            return;
        }
        ShowByIndex(idx);
    }

    public void Next()
    {
        if (cards == null || cards.Length == 0) return;
        int next = (CurrentIndex + 1 + cards.Length) % cards.Length;
        ShowByIndex(next);
    }

    public void Prev()
    {
        if (cards == null || cards.Length == 0) return;
        int prev = (CurrentIndex - 1 + cards.Length) % cards.Length;
        ShowByIndex(prev);
    }

    private int IndexOf(GameObject card)
    {
        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] == card) return i;
        }
        return -1;
    }

    private void RefreshCard(GameObject card)
    {
        if (card == null) return;

        // 找到卡片内所有实现了 ICardRefreshable 的组件并刷新
        var refreshables = card.GetComponentsInChildren<ICardRefreshable>(true);
        foreach (var r in refreshables)
        {
            try { r.Refresh(); }
            catch (System.Exception e)
            {
                Debug.LogError($"{name}: Refresh failed on {r} -> {e.Message}");
            }
        }
    }
}

/// <summary>
/// 可选：让你的 ConfigInputRow / ConfigToggleRow / KeybindRow 实现它，切换卡片时自动刷新显示
/// </summary>
public interface ICardRefreshable
{
    void Refresh();
}
