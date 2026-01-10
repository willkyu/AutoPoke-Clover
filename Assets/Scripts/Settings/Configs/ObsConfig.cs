using System;

[System.Serializable]
public class ObsConfig
{
    public bool enabled = false;
    public string url = "ws://127.0.0.1:4455";
    public string password = "";
    public int connectTimeoutMs = 5000;
    public int requestTimeoutMs = 8000;
    public bool autoReconnect = true;

    public string Get(string field)
    {
        return field switch
        {
            "enabled" => enabled.ToString(),
            "url" => url.ToString(),
            "password" => password.ToString(),
            "connectTimeoutMs" => connectTimeoutMs.ToString(),
            "requestTimeoutMs" => requestTimeoutMs.ToString(),
            "autoReconnect" => autoReconnect.ToString(),
            _ => throw new ArgumentException($"Unknown field: {field}")
        };
    }

    public void Set(string field, string value)
    {
        switch (field)
        {
            case "enabled": enabled = bool.Parse(value); break;
            case "url": url = value; break;
            case "password": password = value; break;
            case "connectTimeoutMs": connectTimeoutMs = int.Parse(value); break;
            case "requestTimeoutMs": requestTimeoutMs = int.Parse(value); break;
            case "autoReconnect": bool.Parse(value); break;
            default: throw new ArgumentException($"Unknown field: {field}");
        }
    }
}
