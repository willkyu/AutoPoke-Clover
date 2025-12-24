using System;
using UnityEngine;

public class KeyRebindListener : MonoBehaviour
{
    public static KeyRebindListener Instance { get; private set; }

    private bool _waiting;
    private GameKey _pendingKey;
    private Action<GameKey, KeyCode> _onRebind;
    private Func<KeyCode, bool> _filter;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        // DontDestroyOnLoad(gameObject);
    }

    public bool IsWaiting => _waiting;

    public void StartRebind(GameKey gameKey,
                            Action<GameKey, KeyCode> onRebind,
                            Func<KeyCode, bool> filter = null)
    {
        _waiting = true;
        _pendingKey = gameKey;
        _onRebind = onRebind;
        _filter = filter;
    }

    public void CancelRebind()
    {
        _waiting = false;
        _onRebind = null;
        _filter = null;
    }

    void Update()
    {
        if (!_waiting) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelRebind();
            return;
        }

        if (!TryGetAnyKeyDown(out var keyCode)) return;

        if (_filter != null && !_filter(keyCode))
            return;

        _waiting = false;
        _onRebind?.Invoke(_pendingKey, keyCode);
        _onRebind = null;
        _filter = null;
    }

    private bool TryGetAnyKeyDown(out KeyCode key)
    {
        foreach (KeyCode k in Enum.GetValues(typeof(KeyCode)))
        {
            if (Input.GetKeyDown(k))
            {
                key = k;
                return true;
            }
        }
        key = KeyCode.None;
        return false;
    }
}
