using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using UnityEngine;



public static class Win32Utils
{
    private static float? scaleRatio = null;

    private static float GetScaleRatio()
    {
        if (scaleRatio.HasValue) return scaleRatio.Value;

        IntPtr hDC = GetDC(IntPtr.Zero);
        int realW = GetDeviceCaps(hDC, DESKTOPHORZRES);
        int apparentW = GetSystemMetrics(0);
        ReleaseDC(IntPtr.Zero, hDC);

        scaleRatio = realW / (float)apparentW;
        Debug.Log($"📐 DPI缩放比: {scaleRatio:F3} ({realW} / {apparentW})");
        return scaleRatio.Value;
    }

    private static RECT GetRealRect(IntPtr hwnd)
    {
        GetWindowRect(hwnd, out RECT rect);
        float ratio = GetScaleRatio();

        return new RECT
        {
            Left = Mathf.RoundToInt(rect.Left * ratio),
            Top = Mathf.RoundToInt(rect.Top * ratio),
            Right = Mathf.RoundToInt(rect.Right * ratio),
            Bottom = Mathf.RoundToInt(rect.Bottom * ratio),
        };
    }

    public static List<IntPtr> FindDesktopChildWindowsWithText(string titleSubstring)
    {
        Debug.Log($"🔍 开始查找包含关键字 \"{titleSubstring}\" 的桌面子窗口...");
        List<IntPtr> matching = new List<IntPtr>();
        IntPtr desktopHwnd = GetDesktopWindow();

        EnumChildWindows(desktopHwnd, (hWnd, lParam) =>
        {
            try
            {
                if (!IsWindowVisible(hWnd)) return true;
                if (GetParent(hWnd) != IntPtr.Zero) return true;

                int len = GetWindowTextLength(hWnd);
                if (len == 0) return true;

                StringBuilder sb = new StringBuilder(len + 1);
                GetWindowText(hWnd, sb, sb.Capacity);
                string title = sb.ToString();

                RECT rc = GetRealRect(hWnd);
                int area = (rc.Right - rc.Left) + (rc.Bottom - rc.Top);

                if (area > 100 && title.Contains(titleSubstring))
                {
                    matching.Add(hWnd);
                    Debug.Log($"✅ 匹配窗口: {title} ({rc.Right - rc.Left}x{rc.Bottom - rc.Top})");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"⚠️ 异常跳过句柄 {hWnd}: {e.Message}");
            }

            return true;
        }, IntPtr.Zero);

        Debug.Log($"🎯 查找结束，共找到 {matching.Count} 个匹配窗口");
        return matching;
    }

    // public static byte[] CaptureWindow(IntPtr hwnd, out int width, out int height)
    // {
    //     RECT rc = GetRealRect(hwnd);
    //     width = rc.Right - rc.Left;
    //     height = rc.Bottom - rc.Top;

    //     IntPtr hWndDC = GetWindowDC(hwnd);
    //     IntPtr hMemDC = CreateCompatibleDC(hWndDC);
    //     IntPtr hBitmap = CreateCompatibleBitmap(hWndDC, width, height);
    //     IntPtr hOld = SelectObject(hMemDC, hBitmap);

    //     BitBlt(hMemDC, 0, 0, width, height, hWndDC, 0, 0, SRCCOPY);

    //     SelectObject(hMemDC, hOld);
    //     DeleteDC(hMemDC);
    //     ReleaseDC(hwnd, hWndDC);

    //     byte[] rawData = GetBitmapBytes(hBitmap, width, height);
    //     DeleteObject(hBitmap);
    //     return rawData;
    // }
    const int HALFTONE = 4;

    public static byte[] CaptureWindow(IntPtr hwnd, out int width, out int height)
    {
        // 原始窗口区域
        RECT rc = GetRealRect(hwnd);
        int srcWidth = rc.Right - rc.Left;
        int srcHeight = rc.Bottom - rc.Top;

        // ✅ 目标缩放分辨率
        width = 480;
        height = 320;

        IntPtr hWndDC = GetWindowDC(hwnd);
        IntPtr hMemDC = CreateCompatibleDC(hWndDC);
        IntPtr hBitmap = CreateCompatibleBitmap(hWndDC, width, height);
        IntPtr hOld = SelectObject(hMemDC, hBitmap);

        // 设置拉伸模式为高质量
        SetStretchBltMode(hMemDC, HALFTONE);

        // ✅ 直接缩放截图
        StretchBlt(hMemDC, 0, 0, width, height,
                   hWndDC, 0, 0, srcWidth, srcHeight,
                   SRCCOPY);

        SelectObject(hMemDC, hOld);
        DeleteDC(hMemDC);
        ReleaseDC(hwnd, hWndDC);

        byte[] rawData = GetBitmapBytes(hBitmap, width, height);
        DeleteObject(hBitmap);
        return rawData;
    }


    private static byte[] GetBitmapBytes(IntPtr hBitmap, int width, int height)
    {
        int bytesPerPixel = 3;
        int stride = ((width * bytesPerPixel + 3) / 4) * 4;
        int totalSize = stride * height;

        BITMAPINFO bmi = new BITMAPINFO();
        bmi.biSize = Marshal.SizeOf(typeof(BITMAPINFO));
        bmi.biWidth = width;
        bmi.biHeight = -height;
        bmi.biPlanes = 1;
        bmi.biBitCount = 24;
        bmi.biCompression = 0;

        byte[] gdiData = new byte[totalSize];
        GCHandle handle = GCHandle.Alloc(gdiData, GCHandleType.Pinned);

        IntPtr hdc = GetDC(IntPtr.Zero);
        GetDIBits(hdc, hBitmap, 0, (uint)height, handle.AddrOfPinnedObject(), ref bmi, 0);
        ReleaseDC(IntPtr.Zero, hdc);
        handle.Free();

        byte[] rawData = new byte[width * height * bytesPerPixel];
        for (int y = 0; y < height; y++)
        {
            Buffer.BlockCopy(
                gdiData, y * stride,
                rawData, y * width * bytesPerPixel,
                width * bytesPerPixel
            );
        }

        for (int i = 0; i < rawData.Length; i += 3)
        {
            byte temp = rawData[i];
            rawData[i] = rawData[i + 2];
            rawData[i + 2] = temp;
        }

        return rawData;
    }

    // public static void SendKey(IntPtr hwnd, KeyCode key)
    // {
    //     Debug.Log($"⌨️ 向窗口 {hwnd} 发送按键: {key}");
    //     PostMessage(hwnd, WM_KEYDOWN, (int)key, 0);
    //     PostMessage(hwnd, WM_KEYUP, (int)key, 0);
    // }
    public static void SendKey(IntPtr hwnd, KeyCode key)
    {
        int vk = KeyCodeToVirtualKey(key);
        if (vk == 0) return;

        Debug.Log($"⌨️ 向窗口 {hwnd} 发送按键: {key} (VK: {vk})");
        SendMessage(hwnd, WM_KEYDOWN, vk, 0);
        Thread.Sleep(100); // ⏱️ 添加 0.1 秒延迟
        SendMessage(hwnd, WM_KEYUP, vk, 0);
    }

    public static void PressKey(IntPtr hwnd, KeyCode key)
    {
        int vk = KeyCodeToVirtualKey(key);
        if (vk == 0) return;

        Debug.Log($"⌨️ 向窗口 {hwnd} 发送按键: {key} (VK: {vk})");
        SendMessage(hwnd, WM_KEYDOWN, vk, 0);
    }

    public static void ReleaseKey(IntPtr hwnd, KeyCode key)
    {
        int vk = KeyCodeToVirtualKey(key);
        if (vk == 0) return;

        Debug.Log($"⌨️ 向窗口 {hwnd} 发送按键: {key} (VK: {vk})");
        SendMessage(hwnd, WM_KEYUP, vk, 0);
    }



    // ------------------ Win32 API ------------------
    [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);
    [DllImport("user32.dll")] private static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);
    [DllImport("user32.dll")] private static extern IntPtr GetDesktopWindow();
    [DllImport("user32.dll")] private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
    [DllImport("user32.dll")] private static extern int GetWindowTextLength(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] private static extern IntPtr GetWindowDC(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr hdc);
    [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);
    [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr hdc);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int width, int height);
    [DllImport("gdi32.dll", SetLastError = true)] private static extern int GetDIBits(IntPtr hdc, IntPtr hBitmap, uint uStartScan, uint cScanLines, IntPtr lpvBits, ref BITMAPINFO lpbi, uint uUsage);
    [DllImport("gdi32.dll")] private static extern bool BitBlt(IntPtr destDC, int x, int y, int width, int height, IntPtr srcDC, int srcX, int srcY, int rop);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr hObject);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern IntPtr GetParent(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);
    [DllImport("gdi32.dll")] private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);
    [DllImport("gdi32.dll")]
    static extern bool StretchBlt(IntPtr hdcDest, int xDest, int yDest, int wDest, int hDest,
                                                         IntPtr hdcSrc, int xSrc, int ySrc, int wSrc, int hSrc, int rop);
    [DllImport("gdi32.dll")] static extern int SetStretchBltMode(IntPtr hdc, int mode);
    [DllImport("user32.dll")] private static extern int GetSystemMetrics(int nIndex);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    public struct BITMAPINFO
    {
        public int biSize;
        public int biWidth;
        public int biHeight;
        public short biPlanes;
        public short biBitCount;
        public int biCompression;
        public int biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public int biClrUsed;
        public int biClrImportant;
    }

    private const int SRCCOPY = 0x00CC0020;
    private const uint WM_KEYDOWN = 0x0100;
    private const uint WM_KEYUP = 0x0101;

    private const int DESKTOPHORZRES = 118;

    public static int KeyCodeToVirtualKey(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.A: return 0x41;
            case KeyCode.B: return 0x42;
            case KeyCode.C: return 0x43;
            case KeyCode.D: return 0x44;
            case KeyCode.E: return 0x45;
            case KeyCode.F: return 0x46;
            case KeyCode.G: return 0x47;
            case KeyCode.H: return 0x48;
            case KeyCode.I: return 0x49;
            case KeyCode.J: return 0x4A;
            case KeyCode.K: return 0x4B;
            case KeyCode.L: return 0x4C;
            case KeyCode.M: return 0x4D;
            case KeyCode.N: return 0x4E;
            case KeyCode.O: return 0x4F;
            case KeyCode.P: return 0x50;
            case KeyCode.Q: return 0x51;
            case KeyCode.R: return 0x52;
            case KeyCode.S: return 0x53;
            case KeyCode.T: return 0x54;
            case KeyCode.U: return 0x55;
            case KeyCode.V: return 0x56;
            case KeyCode.W: return 0x57;
            case KeyCode.X: return 0x58;
            case KeyCode.Y: return 0x59;
            case KeyCode.Z: return 0x5A;

            case KeyCode.Alpha0: return 0x30;
            case KeyCode.Alpha1: return 0x31;
            case KeyCode.Alpha2: return 0x32;
            case KeyCode.Alpha3: return 0x33;
            case KeyCode.Alpha4: return 0x34;
            case KeyCode.Alpha5: return 0x35;
            case KeyCode.Alpha6: return 0x36;
            case KeyCode.Alpha7: return 0x37;
            case KeyCode.Alpha8: return 0x38;
            case KeyCode.Alpha9: return 0x39;

            case KeyCode.Space: return 0x20;
            case KeyCode.Return: return 0x0D;
            case KeyCode.Backspace: return 0x08;
            case KeyCode.Tab: return 0x09;
            case KeyCode.Escape: return 0x1B;

            case KeyCode.F1: return 0x70;
            case KeyCode.F2: return 0x71;
            case KeyCode.F3: return 0x72;
            case KeyCode.F4: return 0x73;
            case KeyCode.F5: return 0x74;
            case KeyCode.F6: return 0x75;
            case KeyCode.F7: return 0x76;
            case KeyCode.F8: return 0x77;
            case KeyCode.F9: return 0x78;
            case KeyCode.F10: return 0x79;
            case KeyCode.F11: return 0x7A;
            case KeyCode.F12: return 0x7B;

            case KeyCode.LeftArrow: return 0x25;
            case KeyCode.UpArrow: return 0x26;
            case KeyCode.RightArrow: return 0x27;
            case KeyCode.DownArrow: return 0x28;

            default:
                Debug.LogWarning($"⚠️ Unmapped KeyCode: {key}");
                return 0; // 不支持的 key 返回 0，可能会被忽略
        }
    }
}



