using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KeybindRow : MonoBehaviour
{
    [Header("Binding")]
    [SerializeField] private GameKey gameKey;

    [Header("UI References")]
    [SerializeField] private TMP_Text bindingText;
    [SerializeField] private Button rebindButton;
    [SerializeField] private TMP_Text hintText; // 可为空
    [SerializeField] private TMP_Text keyText;

    [Header("Options")]
    [SerializeField] private bool allowMouseButtons = false;
    [SerializeField] private bool autoUnbindConflicts = true; // 冲突自动清除

    void Reset()
    {
        // 尽量自动抓一下常用组件，减少手动拖拽
        rebindButton = GetComponentInChildren<Button>();
        var texts = GetComponentsInChildren<TMP_Text>();
        if (texts != null && texts.Length > 0) bindingText = texts[0];
    }

    void Awake()
    {
        if (rebindButton != null)
            rebindButton.onClick.AddListener(OnClickRebind);
        EventManager.I.AddListener(EventName.Refresh, Refresh_);
    }
    void OnDestroy()
    {
        EventManager.I.RemoveListener(EventName.Refresh, Refresh_);
    }

    void OnEnable()
    {
        Refresh();
        if (hintText != null) hintText.text = "";
    }

    public void Refresh()
    {
        if (bindingText == null) return;
        bindingText.text = Settings.Keys.Get(gameKey);
        keyText.text = gameKey.ToString();
    }
    public void Refresh_(object sender, EventArgs args)
    {
        Refresh();
    }

    private void OnClickRebind()
    {
        if (KeyRebindListener.Instance == null)
        {
            Debug.LogError("KeyRebindListener.Instance is null. 请在场景中放置 KeyRebindListener。");
            return;
        }

        if (KeyRebindListener.Instance.IsWaiting)
            return; // 防止同时启动多个重绑

        if (hintText != null) hintText.text = "Press a key... (Esc to cancel)";
        if (rebindButton != null) rebindButton.interactable = false;

        // 过滤规则：是否允许鼠标键等
        bool Filter(KeyCode k)
        {
            if (k == KeyCode.Escape) return false; // Esc 用来取消
            if (!allowMouseButtons && IsMouseKey(k)) return false;
            return true;
        }

        KeyRebindListener.Instance.StartRebind(gameKey, (k, keyCode) =>
        {
            ApplyRebind(k, keyCode);

            if (hintText != null) hintText.text = "";
            if (rebindButton != null) rebindButton.interactable = true;
            Refresh();
        }, Filter);
    }

    private void ApplyRebind(GameKey target, KeyCode newKey)
    {
        if (newKey == KeyCode.None) return;

        var config = Settings.Keys;

        if (autoUnbindConflicts)
        {
            foreach (GameKey k in Enum.GetValues(typeof(GameKey)))
            {
                if (k == target) continue;
                if (config.GetKey(k) == newKey)
                {
                    config.Set(k, KeyCode.None.ToString());
                }
            }
        }

        config.Set(target, newKey.ToString());
        Settings.SaveSettings();
    }

    private bool IsMouseKey(KeyCode k)
    {
        return k == KeyCode.Mouse0 || k == KeyCode.Mouse1 || k == KeyCode.Mouse2 ||
               k == KeyCode.Mouse3 || k == KeyCode.Mouse4 || k == KeyCode.Mouse5 ||
               k == KeyCode.Mouse6;
    }
    public void ResetKey()
    {
        Settings.ResetKeyMapping();
    }
}
