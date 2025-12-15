using Filmder.DTOs;
using Filmder.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Filmder.Controllers;

[ApiController]
[EnableRateLimiting("DefaultBucket")]
public class GameController(IGameService gameService) : ControllerBase
{
    [HttpPost("/createAgame")]
    [Authorize]
    public ActionResult CreateAGame(CreateGameDto createGameDto)
    {
        var game = gameService.CreateGame(createGameDto);
        return Ok(game);
    }

    [HttpPost("/vote")]
    [Authorize]
    public async Task<ActionResult> Vote(VoteDto voteDto)
    {
        await gameService.VoteAsync(voteDto);
        return Ok();
    }

    [HttpGet("/getMoviesBy")]
    public async Task<ActionResult> GetMoviesByCriteria(
        [FromQuery] string? genre,
        [FromQuery] int? releaseDate,
        [FromQuery] int? longestDurationMinutes,
        [FromQuery] int movieCount = 10)
    {
        var result = await gameService.GetMoviesByCriteriaAsync(
            genre,
            releaseDate,
            longestDurationMinutes,
            movieCount
        );

        return Ok(result);
    }

    [HttpGet("/getResults/{gameId}")]
    [Authorize]
    public async Task<ActionResult> GetResults(int gameId)
    {
        var result = await gameService.GetResultsAsync(gameId);
        return Ok(result);
    }

    [HttpPost("/endGame/{gameId}")]
    public async Task<ActionResult> EndGame(int gameId)
    {
        await gameService.EndGameAsync(gameId);
        return Ok("Game ended successfully.");
    }

    [HttpGet("getActiveGame/{groupId}")]
    [Authorize]
    public async Task<ActionResult> GetActiveGames(int groupId)
    {
        var games = await gameService.GetActiveGamesAsync(groupId);
        return Ok(games);
    }

    [HttpGet("getGameResults/{gameId}")]
    [Authorize]
    public async Task<ActionResult> GetGameResults(int gameId)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return BadRequest();

        var result = await gameService.GetGameResultsAsync(gameId, userId);
        return Ok(result);
    }

    [HttpGet("getAllGames/{groupId}")]
    [Authorize]
    public async Task<ActionResult> GetAllGamesByGroup(int groupId)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return BadRequest("User not authenticated");

        var result = await gameService.GetAllGamesByGroupAsync(groupId, userId);
        return Ok(result);
    }
}
