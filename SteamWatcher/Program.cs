#nullable enable

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using SteamWatcher.Infrastructure;

Logger.Info("event=watcher_started");

const string StatusUrl = "http://192.168.1.163:8093/steam/status";

string steamAppsPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
    "Steam",
    "steamapps"
);

var appMap = LoadManifests(steamAppsPath);

Process? workerProcess = null;
string? currentAppId = null;

while (true)
{
    var installDir = DetectRunningGame();

    if (installDir != null)
    {
        var appId = appMap.GetValueOrDefault(installDir);

        if (appId != null && currentAppId != appId)
        {
            Logger.Info($"event=game_started appId={appId} game=\"{installDir}\"");

            workerProcess?.Kill(true);
            workerProcess = StartWorker(appId);
            currentAppId = appId;
        }
    }
    else if (currentAppId != null)
    {
        Logger.Info($"event=game_stopped appId={currentAppId}");
        await SendStatusAsync("idle", currentAppId);

        if (workerProcess != null && !workerProcess.HasExited)
        {
            workerProcess.Kill();
            await workerProcess.WaitForExitAsync();
        }

        workerProcess = null;
        currentAppId = null;
    }

    await Task.Delay(1000);
}

static Process? StartWorker(string appId)
{
    var workerPath = Path.Combine(
        AppContext.BaseDirectory,
        "..",
        "..",
        "..",
        "..",
        "SteamAchievementWorker",
        "bin",
        "Debug",
        "net8.0",
        "SteamAchievementWorker.exe"
    );

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

static Dictionary<string, string> LoadManifests(string steamAppsPath)
{
    var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    foreach (var file in Directory.GetFiles(steamAppsPath, "appmanifest_*.acf"))
    {
        var content = File.ReadAllText(file);

        var appMatch = Regex.Match(content, "\"appid\"\\s+\"(\\d+)\"");
        var dirMatch = Regex.Match(content, "\"installdir\"\\s+\"(.+?)\"");

        if (appMatch.Success && dirMatch.Success)
        {
            map[dirMatch.Groups[1].Value] = appMatch.Groups[1].Value;
        }
    }

    Logger.Info($"event=manifest_loaded count={map.Count}");

    return map;
}

static string? DetectRunningGame()
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
        catch (Exception)
        {
            // Zugriff auf Systemprozesse nicht erlaubt – bewusst ignoriert
        }
    }

    return null;
}

async Task SendStatusAsync(string state, string id)
{
    try
    {
        var payload = new
        {
            state = state,
            game = "",
            appId = id
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var client = new HttpClient();

        var response = await client.PostAsync(StatusUrl, content);

        if (!response.IsSuccessStatusCode)
        {
            Logger.Error($"event=status_http_failed state={state} status={response.StatusCode}");
        }
        else
        {
            Logger.Info($"event=status_sent state={state} appId={id}");
        }
    }
    catch (Exception ex)
    {
        Logger.Error($"event=status_failed error=\"{ex.Message}\"");
    }
}