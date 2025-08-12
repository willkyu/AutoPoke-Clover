using System;

[System.Serializable]
public class NotificationConfig
{
    public bool sendToast = true;
    public bool sendMail = false;
    public string inboxAddress = "";
    public string outboxAddress = "";
    public string outboxSmtpHost = "";
    public string outboxAuthorizationCode = "";

    public string Get(string field)
    {
        return field switch
        {
            "sendToast" => sendToast.ToString(),
            "sendMail" => sendMail.ToString(),
            "inboxAddress" => inboxAddress,
            "outboxAddress" => outboxAddress,
            "outboxSmtpHost" => outboxSmtpHost,
            "outboxAuthorizationCode" => outboxAuthorizationCode,
            _ => throw new ArgumentException($"Unknown field: {field}")
        };
    }

    public void Set(string field, string value)
    {
        switch (field)
        {
            case "sendToast": sendToast = bool.Parse(value); break;
            case "sendMail": sendMail = bool.Parse(value); break;
            case "inboxAddress": inboxAddress = value; break;
            case "outboxAddress": outboxAddress = value; break;
            case "outboxSmtpHost": outboxSmtpHost = value; break;
            case "outboxAuthorizationCode": outboxAuthorizationCode = value; break;
            default: throw new ArgumentException($"Unknown field: {field}");
        }
    }
}
