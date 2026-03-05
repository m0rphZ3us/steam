using System.Diagnostics;

namespace SteamWatcher.Core;

public static class SteamProcessDetector
{
    public static bool IsSteamRunning()
    {
        return Process.GetProcessesByName("steam").Length > 0;
    }
}