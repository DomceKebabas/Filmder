namespace Filmder.DTOs;

public class PersonalizedPlaylistDto
{
    public string PlaylistName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<PlaylistMovieDto> Movies { get; set; } = new();
    public string Reasoning { get; set; } = string.Empty;
}

public class PlaylistMovieDto
{
    public int MovieId { get; set; }
    public string MovieName { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public int ReleaseYear { get; set; }
    public double Rating { get; set; }
    public string PosterUrl { get; set; } = string.Empty;
    public string WhyRecommended { get; set; } = string.Empty;
    public int RecommendationScore { get; set; }
}