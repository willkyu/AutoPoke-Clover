using System;
using UnityEngine;

[Serializable]
public class GeneralConfig
{
    public string APCVersion = "V0.0.1_build1";
    public string gameLanguage = "Eng"; // "Jpn"
    public string gameVersion = "RS";   // "E", "FrLg"
    public string windowName = "Playback";
    public int[] count = new int[] { 0, 0 };
    public bool sendNotification = true;
    public bool autoUpdate = true;
    public bool firstTime = true;

    public string Get(string field)
    {
        return field switch
        {
            "APCVersion" => APCVersion,
            "gameLanguage" => gameLanguage,
            "gameVersion" => gameVersion,
            "windowName" => windowName,
            "sendNotification" => sendNotification.ToString(),
            "autoUpdate" => autoUpdate.ToString(),
            "firstTime" => firstTime.ToString(),
            _ => throw new ArgumentException($"Unknown field: {field}")
        };
    }

    public void Set(string field, string value)
    {
        switch (field)
        {
            case "APCVersion": APCVersion = value; break;
            case "gameLanguage": gameLanguage = value; break;
            case "gameVersion": gameVersion = value; break;
            case "windowName": windowName = value; break;
            case "sendNotification": sendNotification = bool.Parse(value); break;
            case "autoUpdate": autoUpdate = bool.Parse(value); break;
            case "firstTime": firstTime = bool.Parse(value); break;
            default: throw new ArgumentException($"Unknown field: {field}");
        }
    }
}