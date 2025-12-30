# RegisterAumid.ps1
# args: <appId> <exePath> <shortcutName>
$ErrorActionPreference = "Stop"

if ($args.Count -lt 3) {
    throw "Usage: RegisterAumid.ps1 <appId> <exePath> <shortcutName>"
}

$appId = [string]$args[0]
$exePath = [string]$args[1]
$shortcutName = [string]$args[2]

if ([string]::IsNullOrWhiteSpace($appId)) { throw "appId is empty" }
if ([string]::IsNullOrWhiteSpace($exePath)) { throw "exePath is empty" }
if (-not (Test-Path $exePath)) { throw "exePath not found: $exePath" }
if ([string]::IsNullOrWhiteSpace($shortcutName)) { $shortcutName = "AutoPoke" }

# ✅ 当前用户 Start Menu Programs（不需要管理员权限）
$startMenuPrograms = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs"
if (-not (Test-Path $startMenuPrograms)) { New-Item -ItemType Directory -Path $startMenuPrograms | Out-Null }

$lnkPath = Join-Path $startMenuPrograms ($shortcutName + ".lnk")

# 若存在，先删掉（避免被标记只读或旧属性影响）
if (Test-Path $lnkPath) {
    attrib -R $lnkPath 2>$null
    Remove-Item $lnkPath -Force
}

Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

[ComImport]
[Guid("00021401-0000-0000-C000-000000000046")]
public class ShellLink { }

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("000214F9-0000-0000-C000-000000000046")]
public interface IShellLinkW
{
    void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out IntPtr pfd, uint fFlags);
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

[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
public interface IPropertyStore
{
    uint GetCount(out uint cProps);
    uint GetAt(uint iProp, out PROPERTYKEY pkey);
    uint GetValue(ref PROPERTYKEY key, out PROPVARIANT pv);
    uint SetValue(ref PROPERTYKEY key, ref PROPVARIANT pv);
    uint Commit();
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct PROPERTYKEY
{
    public Guid fmtid;
    public uint pid;
    public PROPERTYKEY(Guid f, uint p) { fmtid = f; pid = p; }
}

[StructLayout(LayoutKind.Sequential)]
public struct PROPVARIANT
{
    public ushort vt;
    public ushort wReserved1;
    public ushort wReserved2;
    public ushort wReserved3;
    public IntPtr p;
    public int p2;

    public static PROPVARIANT FromString(string value)
    {
        var pv = new PROPVARIANT();
        pv.vt = 31; // VT_LPWSTR
        pv.p = Marshal.StringToCoTaskMemUni(value);
        return pv;
    }

    public void Clear()
    {
        PropVariantClear(ref this);
    }

    [DllImport("Ole32.dll")]
    static extern int PropVariantClear(ref PROPVARIANT pvar);
}

public static class ShortcutAumidUtil
{
    // PKEY_AppUserModel_ID = {9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3}, 5
    static PROPERTYKEY PKEY_AppUserModel_ID =
        new PROPERTYKEY(new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"), 5);

    public static void CreateShortcutWithAumid(string lnkPath, string exePath, string workDir, string aumid, string desc)
    {
        var link = (IShellLinkW)new ShellLink();
        link.SetPath(exePath);
        if (!string.IsNullOrEmpty(workDir)) link.SetWorkingDirectory(workDir);
        if (!string.IsNullOrEmpty(desc)) link.SetDescription(desc);

        var store = (IPropertyStore)link;
        var pv = PROPVARIANT.FromString(aumid);

        PROPERTYKEY key = PKEY_AppUserModel_ID; // ✅ 需要局部变量才能 ref
        store.SetValue(ref key, ref pv);
        store.Commit();
        pv.Clear();

        var pf = (IPersistFile)link;
        pf.Save(lnkPath, true);
    }
}
"@ | Out-Null

$workDir = Split-Path $exePath
[ShortcutAumidUtil]::CreateShortcutWithAumid($lnkPath, $exePath, $workDir, $appId, $shortcutName)

Write-Output "OK"
Write-Output "Shortcut: $lnkPath"
Write-Output "AUMID: $appId"
Write-Output "Exe: $exePath"
