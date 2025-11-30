using Filmder.Models;

namespace Filmder.MovieParty;

public class WatchParty
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public string HostUserId { get; set; } = null!;

    public string MovieTitle { get; set; } = string.Empty;
    public Platform Platform { get; set; }
    public DateTime ScheduledTime { get; set; }
    public WatchPartyStatus Status { get; set; } = WatchPartyStatus.Scheduled;
    public string? VideoUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Group Group { get; set; } = null!;
    public AppUser Host { get; set; } = null!;
}