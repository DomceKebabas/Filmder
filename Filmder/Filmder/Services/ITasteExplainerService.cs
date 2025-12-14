using Filmder.DTOs;

namespace Filmder.Services;

public interface ITasteExplainerService
{
    Task<TasteExplanationDto?> GetTasteExplanationAsync(string userId);
    Task<object?> GetTasteSummaryAsync(string userId);
    bool HasNoWatchedMovies(List<UserMovieTasteDto> watchedMovies);
    bool HasNoRatedMovies(List<UserMovieTasteDto> watchedMovies);
    TasteExplanationDto GetNoRatingsResponse(List<UserMovieTasteDto> watchedMovies);
}