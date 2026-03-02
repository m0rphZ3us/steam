using System.Diagnostics;

namespace SteamWatcher.Core;

public static class ProcessDetector
{
    public static string? DetectRunningGame()
    {
        foreach (var p in Process.GetProcesses())
        {
            try
            {
                var path = p.MainModule?.FileName;

                if (path != null &&
                    path.Contains(@"\steamapps\common\", StringComparison.OrdinalIgnoreCase) &&
                    !p.ProcessName.Contains("CrashHandler"))
                {
                    return Directory.GetParent(path)!.Name;
                }
            }
            catch
            {
                // Zugriff auf Systemprozesse ignorieren
            }
        }

        return null;
    }
}