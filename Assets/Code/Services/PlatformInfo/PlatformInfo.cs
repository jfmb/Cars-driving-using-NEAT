using UnityEngine;
using System.IO;

public class PlatformInfo
{
    private bool _isPlatformSteamDeck;

    public bool IsPlatformSteamDeck => _isPlatformSteamDeck;

    public void Setup()
    {
        _isPlatformSteamDeck = IsSteamDeck();
    }
    
    private static bool IsSteamDeck()
    {
        return IsLinux() && IsSteamOS() && HasSteamDeckGPU();
//        return true;
    }

    private static bool IsLinux()
    {
        return Application.platform == RuntimePlatform.LinuxPlayer;
    }

    private static bool IsSteamOS()
    {
        string osReleasePath = "/etc/os-release";
        if (File.Exists(osReleasePath))
        {
            string content = File.ReadAllText(osReleasePath).ToLower();
            return content.Contains("steamos");
        }
        return false;
    }

    private static bool HasSteamDeckGPU()
    {
        string gpu = SystemInfo.graphicsDeviceName.ToLower();
        return gpu.Contains("amd") && 
               (gpu.Contains("custom") || gpu.Contains("0405") || gpu.Contains("van gogh"));
    }
}
