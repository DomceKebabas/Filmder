using Filmder.DTOs;
using Filmder.Models;

namespace Filmder.Interfaces;

public interface IGameService
{
    Game CreateGame(CreateGameDto dto);
    Task VoteAsync(VoteDto dto);
    Task<List<Movie>> GetMoviesByCriteriaAsync(string? genre, int? releaseDate, int? longestDurationMinutes, int movieCount);
    Task<List<Movie>> GetResultsAsync(int gameId);
    Task EndGameAsync(int gameId);
    Task<List<Game>> GetActiveGamesAsync(int groupId);
    Task<object> GetGameResultsAsync(int gameId, string userId);
    Task<List<object>> GetAllGamesByGroupAsync(int groupId, string userId);
}