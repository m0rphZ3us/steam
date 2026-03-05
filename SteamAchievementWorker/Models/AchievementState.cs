#nullable enable
namespace SteamAchievementWorker.Models;

public class AchievementState
{
    public string apiName { get; set; } = null!;
    public bool unlocked { get; set; }
    public DateTime? unlockTime { get; set; }
}