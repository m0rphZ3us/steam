using SteamWatcher.Core;
using SteamWatcher.Infrastructure;

Logger.Info("event=watcher_started");

var config = WatcherConfig.Load();
var steamAppsPath = SteamPathResolver.GetSteamAppsPath();
var appMap = ManifestLoader.Load(steamAppsPath);

var watcher = new GameWatcher(config, appMap);

using var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

await watcher.RunAsync(cts.Token);