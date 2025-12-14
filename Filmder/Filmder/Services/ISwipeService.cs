using Filmder.DTOs;
using Filmder.Models;

namespace Filmder.Services;

public interface ISwipeService
{
    Task<(Movie? movie, string? errorMessage)> GetRandomMovieAsync(string userId, string? genre, int? minYear, int? maxDuration);
    Task<(bool success, string message)> RecordSwipeAsync(string userId, SwipeDto swipeDto);
    Task<List<SwipeHistoryDto>> GetSwipeHistoryAsync(string userId, bool? onlyLikes, int page, int pageSize);
    Task<List<Movie>> GetLikedMoviesAsync(string userId, int page, int pageSize);
    Task<(bool success, string message)> DeleteSwipeAsync(string userId, int swipeId);
    Task<SwipeStatsDto> GetSwipeStatsAsync(string userId);
}

public class SwipeStatsDto
{
    public int TotalSwipes { get; set; }
    public int TotalLikes { get; set; }
    public int TotalDislikes { get; set; }
    public double LikePercentage { get; set; }
    public string FavoriteGenre { get; set; } = "None yet";
}