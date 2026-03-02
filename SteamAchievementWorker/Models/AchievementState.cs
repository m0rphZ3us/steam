#nullable enable
namespace SteamAchievementWorker.Models;

public class AchievementState
{
    public string ApiName { get; set; } = null!;
    public bool Unlocked { get; set; }
    public DateTime? UnlockTime { get; set; }
}