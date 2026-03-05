using System;
using System.Collections.Generic;
using System.Threading;
using EasyDevice;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EasyCon : MonoBehaviour
{
    public enum NsButton
    {
        A, B, X, Y,
        L, R, ZL, ZR,
        Plus, Minus,
        Home, Capture,
        Up, Down, Left, Right,
        LStick, RStick
    }

    private enum ConnStatus
    {
        Disconnected,
        Connecting,
        Connected,
        Error
    }

    public static EasyCon Instance { get; private set; }

    [Header("Auto Connect")]
    // [SerializeField] private bool autoConnect = false;
    [SerializeField] private float scanIntervalSeconds = 0.5f;
    [SerializeField] private List<string> portCandidates = new List<string>();
    [SerializeField] private List<int> baudCandidates = new List<int> { 115200, 57600, 38400, 19200 };

    [Header("EasyCon.Device options")]
    [SerializeField] private bool openDelay = true;
    [SerializeField] private int openDelayMs = 200;
    [SerializeField] private bool cpuOpt = true;

    [Header("Input")]
    [SerializeField] private int tapHoldMs = 20;

    private readonly object _stateLock = new object();

    private NintendoSwitch _device;
    private ConnStatus _status = ConnStatus.Disconnected;
    private float _nextScanTime;
    private string _connectedPort = string.Empty;
    private int _connectedBaud;

    public bool IsConnected
    {
        get
        {
            lock (_stateLock)
                return _device != null && _device.IsConnected();
        }
    }

    public string ConnectedPort
    {
        get
        {
            lock (_stateLock)
                return IsConnected ? _connectedPort : string.Empty;
        }
    }

    public int ConnectedBaud
    {
        get
        {
            lock (_stateLock)
                return IsConnected ? _connectedBaud : 0;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        _nextScanTime = Time.unscaledTime;
    }

    private void Update()
    {
        if (IsConnected)
        {
            var port = ConnectedPort;
            if (!string.IsNullOrEmpty(port) && !IsPortStillPresent(port))
                Disconnect();
            return;
        }

        // if (!autoConnect)
        //     return;

        // if (Time.unscaledTime < _nextScanTime)
        //     return;

        // _nextScanTime = Time.unscaledTime + Mathf.Max(0.1f, scanIntervalSeconds);
        // ConnectAuto();
    }

    private void OnDisable()
    {
        Disconnect();
    }

    private void OnDestroy()
    {
        Disconnect();
        if (Instance == this) Instance = null;
    }

    public bool ConnectAuto()
    {
        if (IsConnected) return true;

        var ports = ECDevice.GetPortNames();
        if (ports == null || ports.Count == 0) return false;

        var orderedPorts = BuildOrderedPortList(ports);

        foreach (var port in orderedPorts)
        {
            foreach (var baud in baudCandidates)
            {
                if (Connect(port, baud))
                    return true;
            }
        }

        return false;
    }

    public bool Connect(string portName, int baudRate)
    {
        if (string.IsNullOrWhiteSpace(portName))
            return false;

        lock (_stateLock)
        {
            if (_device != null)
            {
                if (_device.IsConnected() && string.Equals(_connectedPort, portName, StringComparison.OrdinalIgnoreCase))
                    return true;
                _device.Disconnect();
                _device = null;
            }

            _status = ConnStatus.Connecting;

            var dev = new NintendoSwitch();
            dev.SetCpuOpt(cpuOpt);
            dev.SetOpenDelay(openDelay);

            var result = dev.TryConnect(portName);
            if (result == NintendoSwitch.ConnectResult.Success)
            {
                _device = dev;
                _status = ConnStatus.Connected;
                _connectedPort = portName;
                _connectedBaud = baudRate;
                return true;
            }

            _status = ConnStatus.Error;
            _connectedPort = string.Empty;
            _connectedBaud = 0;
            dev.Disconnect();
            return false;
        }
    }

    public void Disconnect()
    {
        lock (_stateLock)
        {
            if (_device != null)
            {
                _device.Disconnect();
                _device = null;
            }

            _status = ConnStatus.Disconnected;
            _connectedPort = string.Empty;
            _connectedBaud = 0;
        }
    }

    public bool SendButtonDown(NsButton button) => ApplyButton(button, true);
    public bool SendButtonUp(NsButton button) => ApplyButton(button, false);
    public bool SendButton(NsButton button, bool pressed) => pressed ? SendButtonDown(button) : SendButtonUp(button);

    public void RefreshController()
    {
        // 连续点击三次 LStick 以唤醒控制器（防止某些情况下第一次按键无响应）
        for (int i = 0; i < 3; i++)
        {
            TapButton(NsButton.LStick);
        }
    }

    public bool TapButton(NsButton button)
    {
        if (!SendButtonDown(button)) return false;

        var holdMs = Math.Max(0, tapHoldMs);
        if (holdMs > 0) Thread.Sleep(holdMs);

        return SendButtonUp(button);
    }

    private bool ApplyButton(NsButton button, bool down)
    {
        NintendoSwitch dev;
        lock (_stateLock)
        {
            dev = _device;
        }

        if (dev == null || !dev.IsConnected())
            return false;

        switch (button)
        {
            case NsButton.Up:
                dev.HatDirection(DirectionKey.Up, down);
                return true;
            case NsButton.Down:
                dev.HatDirection(DirectionKey.Down, down);
                return true;
            case NsButton.Left:
                dev.HatDirection(DirectionKey.Left, down);
                return true;
            case NsButton.Right:
                dev.HatDirection(DirectionKey.Right, down);
                return true;
            default:
                var key = ToECKey(button);
                if (key == null)
                    return false;
                if (down) dev.Down(key);
                else dev.Up(key);
                return true;
        }
    }

    private static ECKey ToECKey(NsButton button)
    {
        switch (button)
        {
            case NsButton.A: return ECKeyUtil.Button(SwitchButton.A);
            case NsButton.B: return ECKeyUtil.Button(SwitchButton.B);
            case NsButton.X: return ECKeyUtil.Button(SwitchButton.X);
            case NsButton.Y: return ECKeyUtil.Button(SwitchButton.Y);
            case NsButton.L: return ECKeyUtil.Button(SwitchButton.L);
            case NsButton.R: return ECKeyUtil.Button(SwitchButton.R);
            case NsButton.ZL: return ECKeyUtil.Button(SwitchButton.ZL);
            case NsButton.ZR: return ECKeyUtil.Button(SwitchButton.ZR);
            case NsButton.Plus: return ECKeyUtil.Button(SwitchButton.PLUS);
            case NsButton.Minus: return ECKeyUtil.Button(SwitchButton.MINUS);
            case NsButton.Home: return ECKeyUtil.Button(SwitchButton.HOME);
            case NsButton.Capture: return ECKeyUtil.Button(SwitchButton.CAPTURE);
            case NsButton.LStick: return ECKeyUtil.Button(SwitchButton.LCLICK);
            case NsButton.RStick: return ECKeyUtil.Button(SwitchButton.RCLICK);
            default: return null;
        }
    }

    private List<string> BuildOrderedPortList(List<string> detected)
    {
        var result = new List<string>(detected.Count);
        var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < portCandidates.Count; i++)
        {
            var candidate = portCandidates[i];
            if (string.IsNullOrWhiteSpace(candidate)) continue;

            for (var j = 0; j < detected.Count; j++)
            {
                var port = detected[j];
                if (!known.Contains(port) && string.Equals(port, candidate, StringComparison.OrdinalIgnoreCase))
                {
                    known.Add(port);
                    result.Add(port);
                    break;
                }
            }
        }

        for (var i = 0; i < detected.Count; i++)
        {
            var port = detected[i];
            if (known.Add(port))
                result.Add(port);
        }

        return result;
    }

    private static bool IsPortStillPresent(string portName)
    {
        var ports = ECDevice.GetPortNames();
        for (var i = 0; i < ports.Count; i++)
        {
            if (string.Equals(ports[i], portName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
