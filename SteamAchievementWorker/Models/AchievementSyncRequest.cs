#nullable enable
namespace SteamAchievementWorker.Models;

public class AchievementSyncRequest
{
    public string AppId { get; set; } = null!;
    public List<AchievementState> Achievements { get; set; } = new();
}