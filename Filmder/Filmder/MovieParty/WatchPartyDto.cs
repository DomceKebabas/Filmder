namespace Filmder.MovieParty;

public class WatchPartyDto
{
    public int Id { get; set; }
    public string MovieTitle { get; set; } = string.Empty;
    public Platform Platform { get; set; }
    public DateTime ScheduledTime { get; set; }
    public WatchPartyStatus Status { get; set; }
    public string? VideoUrl { get; set; }
    public string HostName { get; set; } = string.Empty;
    public string HostUserId { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public int GroupId { get; set; }
    public DateTime CreatedAt { get; set; }
}