#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

public static class ShortcutAumidRegistrar
{
    /// <summary>
    /// 确保开始菜单下存在 shortcut，并且写入 AUMID（AppUserModelID）
    /// </summary>
    public static void EnsureShortcutWithAumid(string shortcutName, string exePath, string aumid)
    {
        if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
            throw new FileNotFoundException("Exe not found", exePath);

        string shortcutPath = GetStartMenuShortcutPath(shortcutName);
        Directory.CreateDirectory(Path.GetDirectoryName(shortcutPath)!);

        // 如果不存在，创建；存在也会覆写 AUMID
        CreateOrUpdateShortcut(shortcutPath, exePath, aumid);
    }

    private static string GetStartMenuShortcutPath(string shortcutName)
    {
        // 当前用户 Start Menu\Programs
        string programs = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
        return Path.Combine(programs, $"{shortcutName}.lnk");
    }

    private static void CreateOrUpdateShortcut(string shortcutPath, string exePath, string aumid)
    {
        // 1) 创建 ShellLink
        var link = (IShellLinkW)new CShellLink();
        link.SetPath(exePath);

        // 可选：工作目录/参数/描述
        string workDir = Path.GetDirectoryName(exePath) ?? "";
        link.SetWorkingDirectory(workDir);
        link.SetDescription("AutoPoke");

        // 2) 写 AUMID (通过 IPropertyStore -> PKEY_AppUserModel_ID)
        var store = (IPropertyStore)link;

        // 关键：你之前遇到 “静态只读字段不能 ref/out”
        // 解决：复制到局部变量再 ref
        PROPERTYKEY key = PROPERTYKEY.PKEY_AppUserModel_ID;

        using (var pv = new PropVariant(aumid))
        {
            store.SetValue(ref key, pv);
            store.Commit();
        }

        // 3) 保存 .lnk
        var pf = (IPersistFile)link;
        pf.Save(shortcutPath, true);

        UnityEngine.Debug.Log($"[Toast] Shortcut saved: {shortcutPath}\nAUMID={aumid}\nEXE={exePath}");
    }

    // ---------------- COM Interop ----------------

    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    private class CShellLink { }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    private interface IShellLinkW
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out WIN32_FIND_DATAW pfd, uint fFlags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
        void Resolve(IntPtr hwnd, uint fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("0000010b-0000-0000-C000-000000000046")]
    private interface IPersistFile
    {
        void GetClassID(out Guid pClassID);
        void IsDirty();
        void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
        void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, bool fRemember);
        void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
        void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
    private interface IPropertyStore
    {
        void GetCount(out uint cProps);
        void GetAt(uint iProp, out PROPERTYKEY pkey);
        void GetValue(ref PROPERTYKEY key, out PropVariant pv);
        void SetValue(ref PROPERTYKEY key, [In] PropVariant pv);
        void Commit();
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WIN32_FIND_DATAW
    {
        public uint dwFileAttributes;
        public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
        public uint nFileSizeHigh;
        public uint nFileSizeLow;
        public uint dwReserved0;
        public uint dwReserved1;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string cFileName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
        public string cAlternateFileName;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROPERTYKEY
    {
        public Guid fmtid;
        public uint pid;

        // System.AppUserModel.ID
        public static readonly PROPERTYKEY PKEY_AppUserModel_ID = new PROPERTYKEY(
            new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"), 5);

        public PROPERTYKEY(Guid fmtid, uint pid)
        {
            this.fmtid = fmtid;
            this.pid = pid;
        }
    }

    // PropVariant（只用 string 即可）
    [StructLayout(LayoutKind.Explicit)]
    private sealed class PropVariant : IDisposable
    {
        [FieldOffset(0)] private ushort vt;
        [FieldOffset(8)] private IntPtr pointerValue;

        private const ushort VT_LPWSTR = 31;

        public PropVariant(string value)
        {
            vt = VT_LPWSTR;
            pointerValue = Marshal.StringToCoTaskMemUni(value);
        }

        public void Dispose()
        {
            PropVariantClear(this);
            GC.SuppressFinalize(this);
        }

        ~PropVariant() => Dispose();

        [DllImport("ole32.dll")]
        private static extern int PropVariantClear([In, Out] PropVariant pvar);
    }
}
#endif
