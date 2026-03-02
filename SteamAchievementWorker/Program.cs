#nullable enable

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Steamworks;
using SteamAchievementWorker.Infrastructure;
using SteamAchievementWorker.Models;

#pragma warning disable S1075
const string SyncUrl = "http://192.168.1.163:8093/steam/achievements/sync";
const string StatusUrl = "http://192.168.1.163:8093/steam/status";
#pragma warning restore S1075

Callback<UserAchievementStored_t>? achievementCallback;

if (args.Length == 0)
{
    Logger.Error("event=missing_appid");
    return;
}

string appId = args[0];

Logger.Info($"event=worker_started appId={appId}");

#pragma warning disable S6966
File.WriteAllText("steam_appid.txt", appId);
#pragma warning restore S6966

if (!SteamAPI.Init())
{
    Logger.Error($"event=steam_init_failed appId={appId}");
    return;
}

Logger.Info($"event=steam_initialized appId={appId}");

await SendStatusAsync("playing", appId);

SteamUserStats.RequestCurrentStats();

achievementCallback =
    Callback<UserAchievementStored_t>.Create(OnAchievementStored);

await SendFullStateAsync(appId);

while (IsGameRunning())
{
    SteamAPI.RunCallbacks();
    Thread.Sleep(1000);
}

SteamAPI.Shutdown();

Logger.Info($"event=worker_shutdown appId={appId}");


// =======================
// Achievement Handling
// =======================

async Task SendFullStateAsync(string currentAppId)
{
    try
    {
        var list = CollectAchievements();

        var request = new AchievementSyncRequest
        {
            AppId = currentAppId,
            Achievements = list
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var client = new HttpClient();
        var response = await client.PostAsync(SyncUrl, content);

        if (!response.IsSuccessStatusCode)
        {
            Logger.Error($"event=achievement_sync_http_failed status={response.StatusCode}");
        }
        else
        {
            Logger.Info($"event=achievement_sync_sent appId={currentAppId} count={list.Count}");
        }
    }
    catch (Exception ex)
    {
        Logger.Error($"event=achievement_sync_failed error=\"{ex.Message}\"");
    }
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

void OnAchievementStored(UserAchievementStored_t callback)
{
    string apiName = callback.m_rgchAchievementName;

    Logger.Info(
        $"event=achievement_unlocked appId={appId} apiName=\"{apiName}\""
    );

    _ = Task.Run(() => SendFullStateAsync(appId));
}

List<AchievementState> CollectAchievements()
{
    var list = new List<AchievementState>();

    uint count = SteamUserStats.GetNumAchievements();

    for (uint i = 0; i < count; i++)
    {
        string apiName = SteamUserStats.GetAchievementName(i);

        if (!SteamUserStats.GetAchievement(apiName, out bool achieved))
            continue;

        DateTime? unlockTime = null;

        if (achieved)
        {
            SteamUserStats.GetAchievementAndUnlockTime(
                apiName,
                out _,
                out uint rawTime
            );

            unlockTime = DateTimeOffset
                .FromUnixTimeSeconds(rawTime)
                .UtcDateTime;
        }

        list.Add(new AchievementState
        {
            ApiName = apiName,
            Unlocked = achieved,
            UnlockTime = unlockTime
        });
    }

    return list;
}

static bool IsGameRunning()
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
                return true;
            }
        }
        catch (Exception)
        {
            // Zugriff auf geschützte Systemprozesse ist normal.
            // Diese Prozesse interessieren uns nicht.
        }
    }

    return false;
}