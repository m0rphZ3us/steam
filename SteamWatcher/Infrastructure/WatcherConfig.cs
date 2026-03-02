using Microsoft.Extensions.Configuration;

namespace SteamWatcher.Infrastructure;

public class WatcherConfig
{
    public string StatusUrl { get; init; } = "";

    public static WatcherConfig Load()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false);

        var config = builder.Build();

        return config.GetSection("Watcher").Get<WatcherConfig>()!;
    }
}