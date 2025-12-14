using Filmder.DTOs;
using Filmder.Interfaces;

namespace Filmder.Services;

public class GuessRatingGameService(IGuessRatingGameRepository repository)
    : IGuessRatingGameService
{
    public async Task<List<object>> GetActiveGamesAsync(int groupId, string userId)
        => await repository.GetActiveGamesAsync(groupId, userId);

    public async Task<List<object>> GetPastGamesAsync(int groupId, string userId)
        => await repository.GetPastGamesAsync(groupId, userId);

    public async Task<List<object>> GetMyGuessesAsync(int gameId, string userId)
        => await repository.GetMyGuessesAsync(gameId, userId);

    public async Task<object> CreateGameAsync(int groupId, string userId)
        => await repository.CreateGameAsync(groupId, userId);

    public async Task<List<object>> GetGameMoviesAsync(int gameId, string userId)
        => await repository.GetGameMoviesAsync(gameId, userId);

    public async Task<object> GuessRatingAsync(int gameId, int movieId, RatingGuessDto dto, string userId)
        => await repository.GuessRatingAsync(gameId, movieId, dto, userId);

    public async Task<object> FinishGameAsync(int gameId, string userId)
        => await repository.FinishGameAsync(gameId, userId);

    public async Task<List<object>> GetGameResultsAsync(int gameId, string userId)
        => await repository.GetGameResultsAsync(gameId, userId);

    public async Task<object> GetGameStatusAsync(int gameId, string userId)
        => await repository.GetGameStatusAsync(gameId, userId);
}