using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Mail;
using System.Net.Mime;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;


public static class MailService
{

    public static bool SendMailShiny(int count, string imgPath)
    {
        EmailFormat emailFormat = new EmailFormat();
        emailFormat.From = Settings.Notification.outboxAddress;
        emailFormat.SmtpClient = Settings.Notification.outboxSmtpHost;
        emailFormat.AuthorizationCode = Settings.Notification.outboxAuthorizationCode;
        emailFormat.To = Settings.Notification.inboxAddress;
        emailFormat.Subject = $"[AutoPoke] Got shiny Pokemon in {count} times!";
        emailFormat.ImagePath = imgPath;
        return _SendEmail(emailFormat);
    }

    public static bool _SendEmail(EmailFormat emailFormat)
    {
        if (string.IsNullOrWhiteSpace(emailFormat.From) ||
            string.IsNullOrWhiteSpace(emailFormat.To) ||
            string.IsNullOrWhiteSpace(emailFormat.SmtpClient) ||
            string.IsNullOrWhiteSpace(emailFormat.AuthorizationCode))
        {
            UnityEngine.Debug.LogError("[Mail] Missing SMTP settings or email fields.");
            return false;
        }

        bool hasImg = !string.IsNullOrWhiteSpace(emailFormat.ImagePath) && File.Exists(emailFormat.ImagePath);
        UnityEngine.Debug.Log($"[Mail] ImagePath = {emailFormat.ImagePath}");
        UnityEngine.Debug.Log($"[Mail] Image exists = {hasImg}");

        using (var mail = new MailMessage())
        {
            mail.From = new MailAddress(emailFormat.From);
            mail.To.Add(emailFormat.To);

            mail.Subject = emailFormat.Subject ?? "";
            mail.SubjectEncoding = Encoding.UTF8;

            string textBody = emailFormat.Body ?? "";
            // 纯文本 view（有些客户端只展示 text/plain 的 alternative）
            var plainView = AlternateView.CreateAlternateViewFromString(textBody, Encoding.UTF8, MediaTypeNames.Text.Plain);
            mail.AlternateViews.Add(plainView);

            // HTML + cid 内嵌图
            string cid = Guid.NewGuid().ToString("N"); // 保证唯一
            string htmlBody =
                "<html><body>" +
                $"<p>{WebUtility.HtmlEncode(textBody).Replace("\n", "<br/>")}</p>" +
                (hasImg ? $"<p><img src=\"cid:{cid}\" style=\"max-width:800px;height:auto;\" /></p>" : "") +
                "</body></html>";

            var htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, Encoding.UTF8, MediaTypeNames.Text.Html);

            if (hasImg)
            {
                string mime = GuessImageMime(emailFormat.ImagePath);
                var lr = new LinkedResource(emailFormat.ImagePath, mime);

                lr.ContentId = cid;
                lr.TransferEncoding = TransferEncoding.Base64;
                lr.ContentType.Name = Path.GetFileName(emailFormat.ImagePath);

                htmlView.LinkedResources.Add(lr);
            }

            // HTML view 放在后面
            mail.AlternateViews.Add(htmlView);

            mail.BodyEncoding = Encoding.UTF8;
            mail.HeadersEncoding = Encoding.UTF8;

            using (var smtp = new SmtpClient(emailFormat.SmtpClient))
            {
                // 你如果用 587/465，建议明确设置
                // smtp.Port = 587;

                smtp.EnableSsl = true;
                smtp.Credentials = new NetworkCredential(emailFormat.From, emailFormat.AuthorizationCode);

                smtp.Send(mail);
            }
        }

        UnityEngine.Debug.Log("[Mail] Sent (with inline image attempt).");
        return true;
    }


    private static string GuessImageMime(string path)
    {
        string ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => MediaTypeNames.Image.Jpeg,
            ".gif" => MediaTypeNames.Image.Gif,
            ".png" => "image/png",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            _ => "image/png"
        };
    }
}

public class EmailFormat
{
    // 账号
    public string From { get; set; }
    // 开启 Smtp 的授权码
    public string AuthorizationCode { get; set; }
    // 那个授权端 QQ，163，或者网易等等
    public string SmtpClient { get; set; }
    // 端口（可要可不要）
    public string Port { get; set; }
    // 邮件主题
    public string Subject { get; set; }
    // 邮件内容
    public string Body { get; set; }
    // 邮件附件
    public string ImagePath { get; set; }
    // 收件人
    public string To { get; set; }

}











public static class ToastService
{
    // 你要固定的 AUMID（必须与注册脚本写入一致）
    public const string DefaultAumid = "willkyu.AutoPoke";
    public const string DefaultShortcutName = "AutoPoke";

    // StreamingAssets/ToastSender/
    private static string BaseDir =>
        Path.Combine(Application.streamingAssetsPath, "ToastSender");

    private static string RegisterPs1 =>
        Path.Combine(BaseDir, "RegisterAumid.ps1");

    private static string ToastPs1 =>
        Path.Combine(BaseDir, "ToastSenderTool.ps1");

    private static int _registered = 0;
    private static int _processAumidSet = 0;

    /// <summary>
    /// 程序启动时调用一次（建议在一个 MonoBehaviour.Start 里调用）
    /// </summary>
    public static void EnsureRegistered(
        string appId = DefaultAumid,
        string shortcutName = DefaultShortcutName)
    {
        if (Interlocked.CompareExchange(ref _registered, 1, 0) != 0) return;

        try
        {
            string exePath = GetPlayerExePath();
            RunRegisterAumidSync(appId, exePath, shortcutName);
            EnsureProcessAumid(appId);

            UnityEngine.Debug.Log($"[Toast] Registered OK. appId={appId}, exe={exePath}");
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"[Toast] EnsureRegistered failed: {e}");
        }
    }

    /// <summary>
    /// 你需要的接口：后台线程也能直接调用
    /// </summary>
    public static void NotifyShiny(int count, string screenshotPath,
        string appId = DefaultAumid)
    {
        string title = "AutoPoke";
        string msg = $"Got Shiny Pokemon in {count} SLs! Congratulations!";
        NotifyWithImage(title, msg, screenshotPath, appId);
    }

    public static void NotifyWithImage(string title, string content, string imagePath,
        string appId = DefaultAumid,
        string shortcutName = DefaultShortcutName)
    {
        // 保证注册 + 当前进程AUMID
        EnsureRegistered(appId, shortcutName);

        ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                RunToastSync(appId, title, content, imagePath);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[Toast] Notify failed: {e}");
            }
        });
    }

    // ----------------- 核心：给当前进程设置 AUMID -----------------
    private static void EnsureProcessAumid(string appId)
    {
        if (Interlocked.CompareExchange(ref _processAumidSet, 1, 0) != 0) return;

        int hr = SetCurrentProcessExplicitAppUserModelID(appId);
        // S_OK = 0
        if (hr != 0)
        {
            UnityEngine.Debug.LogWarning($"[Toast] SetCurrentProcessExplicitAppUserModelID failed. hr=0x{hr:X8}");
        }
    }

    [System.Runtime.InteropServices.DllImport("shell32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern int SetCurrentProcessExplicitAppUserModelID(string AppID);

    // ----------------- PS 调用 -----------------
    private static void RunRegisterAumidSync(string appId, string exePath, string shortcutName)
    {
        if (!File.Exists(RegisterPs1))
            throw new FileNotFoundException("RegisterAumid.ps1 not found", RegisterPs1);

        // RegisterAumid.ps1 <appId> <exePath> <shortcutName>
        string args = $"-NoProfile -ExecutionPolicy Bypass -STA -File \"{RegisterPs1}\" " +
                      $"\"{appId}\" \"{exePath}\" \"{shortcutName}\"";

        RunPowerShellRawSync(args);
    }

    private static void RunToastSync(string appId, string title, string content, string imagePath)
    {
        if (!File.Exists(ToastPs1))
            throw new FileNotFoundException("ToastSenderTool.ps1 not found", ToastPs1);

        // ToastSenderTool.ps1 <appId> <title> <content> <imagePath>
        string args = $"-NoProfile -ExecutionPolicy Bypass -STA -File \"{ToastPs1}\" " +
                      $"\"{appId}\" \"{title}\" \"{content}\" \"{imagePath}\"";

        RunPowerShellRawSync(args);
    }

    private static void RunPowerShellRawSync(string psArguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = psArguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        using var p = Process.Start(psi);
        string stdout = p.StandardOutput.ReadToEnd();
        string stderr = p.StandardError.ReadToEnd();
        p.WaitForExit();

        if (!string.IsNullOrWhiteSpace(stdout))
            UnityEngine.Debug.Log("[Toast] PS out:\n" + stdout);

        if (!string.IsNullOrWhiteSpace(stderr))
            throw new Exception("[Toast] PS error:\n" + stderr);
    }

    private static string GetPlayerExePath()
    {
#if UNITY_EDITOR
        // Editor 下只是为了测试注册流程；真正发 toast 建议用 Player 测
        return Process.GetCurrentProcess().MainModule?.FileName ?? "";
#else
        string dir = Path.GetDirectoryName(Application.dataPath) ?? "";
        string exe = Application.productName + ".exe";
        return Path.Combine(dir, exe);
#endif
    }
}