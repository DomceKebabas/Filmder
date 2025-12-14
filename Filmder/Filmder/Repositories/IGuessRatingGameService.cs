using Filmder.DTOs;

namespace Filmder.Interfaces;

public interface IGuessRatingGameRepository
{
    Task<List<object>> GetActiveGamesAsync(int groupId, string userId);
    Task<List<object>> GetPastGamesAsync(int groupId, string userId);
    Task<List<object>> GetMyGuessesAsync(int gameId, string userId);
    Task<object> CreateGameAsync(int groupId, string userId);
    Task<List<object>> GetGameMoviesAsync(int gameId, string userId);
    Task<object> GuessRatingAsync(int gameId, int movieId, RatingGuessDto dto, string userId);
    Task<object> FinishGameAsync(int gameId, string userId);
    Task<List<object>> GetGameResultsAsync(int gameId, string userId);
    Task<object> GetGameStatusAsync(int gameId, string userId);
}