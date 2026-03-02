using System.Diagnostics;
using SteamWatcher.Infrastructure;

namespace SteamWatcher.Core;

public static class WorkerLauncher
{
    public static Process? Start(string appId)
    {
        var workerPath = ResolveWorkerPath();

        if (!File.Exists(workerPath))
        {
            Logger.Error("event=worker_not_found");
            return null;
        }

        return Process.Start(new ProcessStartInfo
        {
            FileName = workerPath,
            Arguments = appId,
            UseShellExecute = false
        });
    }

    private static string ResolveWorkerPath()
    {
        return Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "SteamAchievementWorker",
            "bin", "Debug", "net8.0",
            "SteamAchievementWorker.exe"
        );
    }
}