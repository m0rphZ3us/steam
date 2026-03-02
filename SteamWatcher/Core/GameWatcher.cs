using System.Diagnostics;
using SteamWatcher.Infrastructure;

namespace SteamWatcher.Core;

public class GameWatcher
{
    private readonly WatcherConfig _config;
    private readonly Dictionary<string, string> _appMap;

    private Process? _workerProcess;
    private string? _currentAppId;

    public GameWatcher(WatcherConfig config, Dictionary<string, string> appMap)
    {
        _config = config;
        _appMap = appMap;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await CheckGameStateAsync();
            await Task.Delay(1000, cancellationToken);
        }

        Logger.Info("event=watcher_stopped");
    }

    private async Task CheckGameStateAsync()
    {
        var installDir = ProcessDetector.DetectRunningGame();

        if (installDir == null)
        {
            await HandleGameStoppedAsync();
            return;
        }

        if (!_appMap.TryGetValue(installDir, out var appId))
            return;

        if (_currentAppId == appId)
            return;

        StartNewGame(appId, installDir);
    }

    private void StartNewGame(string appId, string installDir)
    {
        Logger.Info($"event=game_started appId={appId} game=\"{installDir}\"");

        _workerProcess?.Kill(true);
        _workerProcess = WorkerLauncher.Start(appId);

        _currentAppId = appId;
    }

    private async Task HandleGameStoppedAsync()
    {
        if (_currentAppId == null)
            return;

        Logger.Info($"event=game_stopped appId={_currentAppId}");

        await StatusClient.SendAsync(_config.StatusUrl, "idle", _currentAppId);

        if (_workerProcess is { HasExited: false })
        {
            _workerProcess.Kill();
            await _workerProcess.WaitForExitAsync();
        }

        _workerProcess = null;
        _currentAppId = null;
    }
}