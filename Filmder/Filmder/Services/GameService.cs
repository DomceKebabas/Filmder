using Filmder.Data;
using Filmder.DTOs;
using Filmder.Extensions;
using Filmder.Interfaces;
using Filmder.Models;
using Filmder.Signal;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Filmder.Services;

public class GameService(
    IGameRepository gameRepository,
    IHubContext<ChatHub> hubContext
) : IGameService
{
    public Game CreateGame(CreateGameDto dto)
    {
        return gameRepository.CreateGame(dto);
    }

    public async Task VoteAsync(VoteDto dto)
    {
        await gameRepository.VoteAsync(dto);
    }

    public async Task<List<Movie>> GetMoviesByCriteriaAsync(
        string? genre,
        int? releaseDate,
        int? longestDurationMinutes,
        int movieCount)
    {
        return await gameRepository.GetMoviesByCriteriaAsync(
            genre,
            releaseDate,
            longestDurationMinutes,
            movieCount
        );
    }

    public async Task<List<Movie>> GetResultsAsync(int gameId)
    {
        return await gameRepository.GetResultsAsync(gameId);
    }

    public async Task EndGameAsync(int gameId)
    {
        var game = await gameRepository.EndGameAsync(gameId);

        await hubContext.Clients.Group(gameId.ToString())
            .SendAsync("gameEnded", $"🎬 Game {game.Name} has ended! View results now.");
    }

    public async Task<List<Game>> GetActiveGamesAsync(int groupId)
    {
        return await gameRepository.GetActiveGamesAsync(groupId);
    }

    public async Task<object> GetGameResultsAsync(int gameId, string userId)
    {
        return await gameRepository.GetGameResultsAsync(gameId);
    }

    public async Task<List<object>> GetAllGamesByGroupAsync(int groupId, string userId)
    {
        return await gameRepository.GetAllGamesByGroupAsync(groupId, userId);
    }
}