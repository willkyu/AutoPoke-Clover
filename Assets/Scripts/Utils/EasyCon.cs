using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EasyCon : MonoBehaviour
{
    public enum NsButton
    {
        A,
        B,
        X,
        Y,
        L,
        R,
        ZL,
        ZR,
        Plus,
        Minus,
        Home,
        Capture,
        Up,
        Down,
        Left,
        Right,
        LStick,
        RStick
    }

    public static EasyCon Instance { get; private set; }

    [Header("Auto Connect")]
    [SerializeField] private bool autoConnect = true;
    [SerializeField] private float scanIntervalSeconds = 0.5f;
    [SerializeField] private List<string> portCandidates = new List<string>();
    [SerializeField] private List<int> baudCandidates = new List<int> { 115200, 57600, 38400, 19200 };

    [Header("Serial")]
    [SerializeField] private int writeTimeoutMs = 10;
    [SerializeField] private int readTimeoutMs = 10;
    [SerializeField] private int connectWriteRetryWindowMs = 120;
    [SerializeField] private int connectWriteRetryIntervalMs = 2;
    [SerializeField] private int tapHoldMs = 20;

    private static readonly byte[] ConnectHelloCmd = { 0xA5, 0xA5, 0x81 };
    private const byte ConnectHelloReply = 0x80;
    private const byte HatTop = 0x00;
    private const byte HatTopRight = 0x01;
    private const byte HatRight = 0x02;
    private const byte HatBottomRight = 0x03;
    private const byte HatBottom = 0x04;
    private const byte HatBottomLeft = 0x05;
    private const byte HatLeft = 0x06;
    private const byte HatTopLeft = 0x07;
    private const byte HatCenter = 0x08;

    private const byte DpadUpBit = 1 << 0;
    private const byte DpadDownBit = 1 << 1;
    private const byte DpadLeftBit = 1 << 2;
    private const byte DpadRightBit = 1 << 3;
    private static readonly byte[] DpadToHat =
    {
        HatCenter,      // 0000
        HatTop,         // 0001 U
        HatBottom,      // 0010 D
        HatCenter,      // 0011 U+D
        HatLeft,        // 0100 L
        HatTopLeft,     // 0101 U+L
        HatBottomLeft,  // 0110 D+L
        HatLeft,        // 0111 U+D+L
        HatRight,       // 1000 R
        HatTopRight,    // 1001 U+R
        HatBottomRight, // 1010 D+R
        HatRight,       // 1011 U+D+R
        HatCenter,      // 1100 L+R
        HatTop,         // 1101 U+L+R
        HatBottom,      // 1110 D+L+R
        HatCenter       // 1111 U+D+L+R
    };

    private const ushort SwitchY = 0x0001;
    private const ushort SwitchB = 0x0002;
    private const ushort SwitchA = 0x0004;
    private const ushort SwitchX = 0x0008;
    private const ushort SwitchL = 0x0010;
    private const ushort SwitchR = 0x0020;
    private const ushort SwitchZL = 0x0040;
    private const ushort SwitchZR = 0x0080;
    private const ushort SwitchMinus = 0x0100;
    private const ushort SwitchPlus = 0x0200;
    private const ushort SwitchLClick = 0x0400;
    private const ushort SwitchRClick = 0x0800;
    private const ushort SwitchHome = 0x1000;
    private const ushort SwitchCapture = 0x2000;
    private static readonly ushort[] ButtonMasks =
    {
        SwitchA,       // A
        SwitchB,       // B
        SwitchX,       // X
        SwitchY,       // Y
        SwitchL,       // L
        SwitchR,       // R
        SwitchZL,      // ZL
        SwitchZR,      // ZR
        SwitchPlus,    // Plus
        SwitchMinus,   // Minus
        SwitchHome,    // Home
        SwitchCapture, // Capture
        0,             // Up
        0,             // Down
        0,             // Left
        0,             // Right
        SwitchLClick,  // LStick
        SwitchRClick   // RStick
    };

    private SerialPort _serial;
    private float _nextScanTime;
    private long _connectWriteRetryUntilMs;
    private ushort _buttonState;
    private byte _dpadState;
    private byte _hatState = HatCenter;
    private readonly byte[] _reportPacket = new byte[8];

    public bool IsConnected => _serial != null && _serial.IsOpen;
    public string ConnectedPort => IsConnected ? _serial.PortName : string.Empty;
    public int ConnectedBaud => IsConnected ? _serial.BaudRate : 0;

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
            if (!IsPortStillPresent(_serial.PortName))
            {
                Disconnect();
            }

            return;
        }

        if (!autoConnect || Time.unscaledTime < _nextScanTime)
        {
            return;
        }

        _nextScanTime = Time.unscaledTime + Mathf.Max(0.1f, scanIntervalSeconds);
        ConnectAuto();
    }

    private void OnDisable()
    {
        Disconnect();
    }

    private void OnDestroy()
    {
        Disconnect();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public bool ConnectAuto()
    {
        if (IsConnected)
        {
            return true;
        }

        var ports = SerialPort.GetPortNames();
        if (ports == null || ports.Length == 0)
        {
            return false;
        }

        var orderedPorts = BuildOrderedPortList(ports);
        for (var i = 0; i < orderedPorts.Count; i++)
        {
            var port = orderedPorts[i];
            for (var j = 0; j < baudCandidates.Count; j++)
            {
                if (!TryConnect(port, baudCandidates[j]))
                {
                    continue;
                }

                if (TryHandshakeHello())
                {
                    return true;
                }

                Disconnect();
            }
        }

        return false;
    }

    public bool Connect(string portName, int baudRate)
    {
        if (IsConnected)
        {
            if (string.Equals(_serial.PortName, portName, StringComparison.OrdinalIgnoreCase) && _serial.BaudRate == baudRate)
            {
                return true;
            }

            Disconnect();
        }

        return TryConnect(portName, baudRate);
    }

    public void Disconnect()
    {
        if (_serial == null)
        {
            return;
        }

        try
        {
            if (_serial.IsOpen)
            {
                _serial.Close();
            }
        }
        catch
        {
            // Ignore close failures; the port may already be gone.
        }
        finally
        {
            _serial.Dispose();
            _serial = null;
        }
    }

    public bool SendButtonDown(NsButton button)
    {
        return ApplyButtonState(button, true);
    }

    public bool SendButtonUp(NsButton button)
    {
        return ApplyButtonState(button, false);
    }

    public bool SendButton(NsButton button, bool pressed)
    {
        return pressed ? SendButtonDown(button) : SendButtonUp(button);
    }

    public bool TapButton(NsButton button)
    {
        if (!SendButtonDown(button))
        {
            return false;
        }

        var holdMs = Math.Max(0, tapHoldMs);
        if (holdMs > 0)
        {
            Thread.Sleep(holdMs);
        }

        return SendButtonUp(button);
    }

    private bool TryConnect(string portName, int baudRate)
    {
        SerialPort trial = null;
        try
        {
            trial = new SerialPort(portName, baudRate)
            {
                DtrEnable = true,
                RtsEnable = true,
                ReadTimeout = readTimeoutMs,
                WriteTimeout = writeTimeoutMs,
                NewLine = "\n"
            };

            trial.Open();
            _serial = trial;
            _connectWriteRetryUntilMs = Environment.TickCount + Math.Max(0, connectWriteRetryWindowMs);
            return true;
        }
        catch
        {
            if (trial != null)
            {
                try
                {
                    if (trial.IsOpen)
                    {
                        trial.Close();
                    }
                }
                catch
                {
                    // Ignore failures while cleaning up unsuccessful open.
                }

                trial.Dispose();
            }

            return false;
        }
    }

    private bool SendRaw(byte[] data)
    {
        if (!IsConnected)
        {
            return false;
        }

        var retryInterval = Math.Max(0, connectWriteRetryIntervalMs);
        while (true)
        {
            try
            {
                _serial.Write(data, 0, data.Length);
                return true;
            }
            catch (TimeoutException)
            {
                if (Environment.TickCount > _connectWriteRetryUntilMs)
                {
                    Disconnect();
                    return false;
                }

                if (retryInterval > 0)
                {
                    Thread.Sleep(retryInterval);
                }
            }
            catch
            {
                Disconnect();
                return false;
            }
        }
    }

    private bool TryHandshakeHello()
    {
        if (!IsConnected)
        {
            return false;
        }

        try
        {
            _serial.DiscardInBuffer();
            _serial.DiscardOutBuffer();
            _serial.Write(ConnectHelloCmd, 0, ConnectHelloCmd.Length);

            var reply = _serial.ReadByte();
            return reply == ConnectHelloReply;
        }
        catch
        {
            return false;
        }
    }

    private bool ApplyButtonState(NsButton button, bool pressed)
    {
        var mask = ToSwitchMask(button);
        if (mask != 0)
        {
            if (pressed)
            {
                _buttonState |= mask;
            }
            else
            {
                _buttonState = (ushort)(_buttonState & ~mask);
            }

            return SendCurrentReport();
        }

        switch (button)
        {
            case NsButton.Up:
                SetDpadBit(DpadUpBit, pressed);
                break;
            case NsButton.Down:
                SetDpadBit(DpadDownBit, pressed);
                break;
            case NsButton.Left:
                SetDpadBit(DpadLeftBit, pressed);
                break;
            case NsButton.Right:
                SetDpadBit(DpadRightBit, pressed);
                break;
            default:
                return false;
        }

        _hatState = ComputeHat(_dpadState);
        return SendCurrentReport();
    }

    private void SetDpadBit(byte bit, bool pressed)
    {
        if (pressed)
        {
            _dpadState |= bit;
        }
        else
        {
            _dpadState = (byte)(_dpadState & ~bit);
        }
    }

    private bool SendCurrentReport()
    {
        const byte stickCenter = 128;
        PackReport(_buttonState, _hatState, stickCenter, stickCenter, stickCenter, stickCenter, _reportPacket);
        return SendRaw(_reportPacket);
    }

    private static ushort ToSwitchMask(NsButton button)
    {
        var index = (int)button;
        if ((uint)index < (uint)ButtonMasks.Length)
        {
            return ButtonMasks[index];
        }

        return 0;
    }

    private static byte ComputeHat(byte dpadState)
    {
        return DpadToHat[dpadState & 0x0F];
    }

    private static void PackReport(ushort buttons, byte hat, byte lx, byte ly, byte rx, byte ry, byte[] output)
    {
        var payload = ((ulong)buttons << 40) |
                      ((ulong)hat << 32) |
                      ((ulong)lx << 24) |
                      ((ulong)ly << 16) |
                      ((ulong)rx << 8) |
                      ry;

        for (var i = 0; i < 8; i++)
        {
            var shift = 49 - (i * 7);
            output[i] = (byte)((payload >> shift) & 0x7F);
        }

        output[7] |= 0x80;
    }

    private List<string> BuildOrderedPortList(string[] detected)
    {
        var result = new List<string>(detected.Length);
        var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < portCandidates.Count; i++)
        {
            var candidate = portCandidates[i];
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            for (var j = 0; j < detected.Length; j++)
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

        for (var i = 0; i < detected.Length; i++)
        {
            var port = detected[i];
            if (known.Add(port))
            {
                result.Add(port);
            }
        }

        return result;
    }

    private static bool IsPortStillPresent(string portName)
    {
        var ports = SerialPort.GetPortNames();
        for (var i = 0; i < ports.Length; i++)
        {
            if (string.Equals(ports[i], portName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
