using System.Text.RegularExpressions;
using SteamWatcher.Infrastructure;

namespace SteamWatcher.Core;

public static class ManifestLoader
{
    public static Dictionary<string, string> Load(string steamAppsPath)
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
}