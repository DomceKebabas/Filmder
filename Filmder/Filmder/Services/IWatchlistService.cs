using Filmder.DTOs;

namespace Filmder.Services;

public interface IWatchlistService
{
    Task<List<WatchlistMovieDto>> GenerateWatchlistAsync(string userId, int count);
    Task<UserPreferencesDto> GetUserPreferencesAsync(string userId);
}