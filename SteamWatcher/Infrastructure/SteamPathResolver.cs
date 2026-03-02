namespace SteamWatcher.Infrastructure;

public static class SteamPathResolver
{
    public static string GetSteamAppsPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "Steam",
            "steamapps"
        );
    }
}