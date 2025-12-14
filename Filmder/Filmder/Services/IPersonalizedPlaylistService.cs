using Filmder.DTOs;

namespace Filmder.Services;

public interface IPersonalizedPlaylistService
{
    Task<PersonalizedPlaylistResultDto> GeneratePlaylistAsync(string userId, int count);
    Task<QuickPicksResultDto> GetQuickPicksAsync(string userId);
}

public class PersonalizedPlaylistResultDto
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int? StatusCode { get; set; }
    public PersonalizedPlaylistDto? Playlist { get; set; }
}

public class QuickPicksResultDto
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int? StatusCode { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public List<QuickPickMovieDto>? Movies { get; set; }
}

public class QuickPickMovieDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Genre { get; set; } = null!;
    public int ReleaseYear { get; set; }
    public double Rating { get; set; }
    public string? PosterUrl { get; set; }
    public int Duration { get; set; }
    public string? Director { get; set; }
}