using Filmder.DTOs;

namespace Filmder.Services;

public interface IRatingService
{
    Task<(bool Success, string Message)> RateMovieAsync(string userId, RateMovieDto dto);
    Task<List<RatingResponseDto>> GetRatingsByMovieAsync(int movieId);
    Task<AverageRatingDto> GetAverageRatingAsync(int movieId);
}

public class RatingResponseDto
{
    public int Id { get; set; }
    public int Score { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public string UserEmail { get; set; } = null!;
}

public class AverageRatingDto
{
    public double AverageScore { get; set; }
    public int TotalRatings { get; set; }
}