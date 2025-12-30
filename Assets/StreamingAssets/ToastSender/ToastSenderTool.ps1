# ToastSenderTool.ps1
# args: <appId> <title> <content> <imagePath>
$ErrorActionPreference = "Stop"

if ($args.Count -lt 3) { throw "Usage: ToastSenderTool.ps1 <appId> <title> <content> <imagePath(optional)>" }

$appId   = [string]$args[0]
$title   = [string]$args[1]
$content = [string]$args[2]
$imagePath = ""
if ($args.Count -ge 4) { $imagePath = [string]$args[3] }

# WinRT types
[Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] | Out-Null
[Windows.Data.Xml.Dom.XmlDocument, Windows.Data.Xml.Dom.XmlDocument, ContentType = WindowsRuntime] | Out-Null

function EscapeXml([string]$s) {
    if ($null -eq $s) { return "" }
    return $s.Replace("&","&amp;").Replace("<","&lt;").Replace(">","&gt;").Replace("'","&apos;").Replace('"',"&quot;")
}

$t = EscapeXml $title
$c = EscapeXml $content

$imageBlock = ""
if ($imagePath -and (Test-Path $imagePath)) {
    $full = (Resolve-Path $imagePath).Path
    $uri = "file:///" + ($full -replace "\\","/")
    $imageBlock = "<image placement='appLogoOverride' src='$uri' hint-crop='circle'/>`n<image placement='hero' src='$uri'/>"
}

$template = @"
<toast>
  <visual>
    <binding template="ToastGeneric">
      <text>$t</text>
      <text>$c</text>
      $imageBlock
    </binding>
  </visual>
</toast>
"@

$toastXml = New-Object Windows.Data.Xml.Dom.XmlDocument
$toastXml.LoadXml($template)
$toast = [Windows.UI.Notifications.ToastNotification]::new($toastXml)

$notifier = [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier($appId)
if ($null -eq $notifier) {
    throw "CreateToastNotifier returned null. Check appId(AUMID) and registration (Start Menu shortcut). appId=$appId"
}

$notifier.Show($toast)
"OK"
