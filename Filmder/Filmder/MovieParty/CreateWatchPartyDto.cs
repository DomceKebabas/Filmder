namespace Filmder.MovieParty;

public class CreateWatchPartyDto
{
    public string MovieTitle { get; set; } = string.Empty;
    public Platform Platform { get; set; }
    public DateTime ScheduledTime { get; set; }
    public string? VideoUrl { get; set; }
}