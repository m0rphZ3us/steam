#nullable enable
using System.Text;

namespace SteamAchievementWorker.Infrastructure;

public static class Logger
{
    #pragma warning disable S1075
    private const string LogFilePath = @"C:\steam-logs\steam.log";
    #pragma warning restore S1075
    public static void Info(string message)
        => Write("INFO", message);

    public static void Warn(string message)
        => Write("WARN", message);

    public static void Error(string message)
        => Write("ERROR", message);

    private static void Write(string level, string message)
    {
        var line = $"{level} 1 --- [steam] SteamAchievementWorker : {message}";

        Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath)!);
        File.AppendAllText(LogFilePath, line + Environment.NewLine, Encoding.UTF8);
    }
}