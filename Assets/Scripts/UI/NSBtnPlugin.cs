using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(Image))]
public class NSBtnPlugin : MonoBehaviour
{

    private Button btn;
    private Image image;
    private Color originalColor;
    private bool easyConConnected;

    private static readonly Color DisabledColor = Color.gray;
    private static readonly Color DisconnectedColor = Color.red;

    private void Awake()
    {
        btn = GetComponent<Button>();
        image = GetComponent<Image>();

        originalColor = image.color;
        easyConConnected = EasyCon.Instance != null && EasyCon.Instance.IsConnected;

        btn.onClick.AddListener(OnClick);
        EventManager.I.AddListener(EventName.SetEasyConState, OnEasyConStateChanged);
        EventManager.I.AddListener(EventName.Refresh, OnSettingsRefreshed);

        UpdateVisual();
    }

    private void OnDestroy()
    {
        EventManager.I.RemoveListener(EventName.SetEasyConState, OnEasyConStateChanged);
        EventManager.I.RemoveListener(EventName.Refresh, OnSettingsRefreshed);

        if (btn != null)
        {
            btn.onClick.RemoveListener(OnClick);
        }
    }

    private void OnClick()
    {
        Settings.Current.ifNS = !Settings.Current.ifNS;
        Settings.SaveSettings();
        APCore.I.RefreshEasyCon();
        UpdateVisual();
    }

    private void OnEasyConStateChanged(object sender, EventArgs args)
    {
        var data = args as SetEasyConStateEventArgs;
        if (data == null)
        {
            return;
        }

        easyConConnected = data.isConnected;
        UpdateVisual();
    }

    private void OnSettingsRefreshed(object sender, EventArgs args)
    {
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (!Settings.Current.ifNS)
        {
            image.color = DisabledColor;
            return;
        }

        image.color = easyConConnected ? originalColor : DisconnectedColor;
    }
}
