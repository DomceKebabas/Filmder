namespace Filmder.DTOs;

public class UserMovieTasteDto
{
    public string MovieName { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public int ReleaseYear { get; set; }
    public string Director { get; set; } = string.Empty;
    public int? UserRating { get; set; }
    public string? UserComment { get; set; }
    public DateTime WatchedAt { get; set; }
}