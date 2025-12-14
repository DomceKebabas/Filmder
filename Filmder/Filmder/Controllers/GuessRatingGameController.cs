using Filmder.DTOs;
using Filmder.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace Filmder.Controllers;

[EnableRateLimiting("DefaultBucket")]
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GuessRatingGameController(IGuessRatingGameService gameService) : ControllerBase
{
    [HttpGet("groups/{groupId}/active-games")]
    public async Task<ActionResult> GetActiveGames(int groupId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest("User not authenticated");

        var result = await gameService.GetActiveGamesAsync(groupId, userId);
        return Ok(result);
    }

    [HttpGet("groups/{groupId}/past-games")]
    public async Task<ActionResult> GetPastGames(int groupId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest("User not authenticated");

        var result = await gameService.GetPastGamesAsync(groupId, userId);
        return Ok(result);
    }

    [HttpGet("games/{gameId}/my-guesses")]
    public async Task<ActionResult> GetMyGuesses(int gameId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest("User not authenticated");

        var result = await gameService.GetMyGuessesAsync(gameId, userId);
        return Ok(result);
    }

    [HttpPost("groups/{groupId}/guessing-games")]
    public async Task<ActionResult> CreateGame(int groupId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest("User not authenticated");

        var result = await gameService.CreateGameAsync(groupId, userId);
        return Ok(result);
    }

    [HttpGet("games/{gameId}/guessing-games")]
    public async Task<ActionResult> GetGameMovies(int gameId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest("User not authenticated");

        var result = await gameService.GetGameMoviesAsync(gameId, userId);
        return Ok(result);
    }

    [HttpPost("games/{gameId}/movies/{movieId}/guesses")]
    public async Task<ActionResult> GuessRating(int gameId, int movieId, RatingGuessDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest("User not authenticated");

        var result = await gameService.GuessRatingAsync(gameId, movieId, dto, userId);
        return Ok(result);
    }

    [HttpPost("games/{gameId}/finish")]
    public async Task<ActionResult> FinishGame(int gameId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest("User not authenticated");

        var result = await gameService.FinishGameAsync(gameId, userId);
        return Ok(result);
    }

    [HttpGet("games/{gameId}/results")]
    public async Task<ActionResult> GetGameResults(int gameId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest("User not authenticated");

        var result = await gameService.GetGameResultsAsync(gameId, userId);
        return Ok(result);
    }

    [HttpGet("games/{gameId}/status")]
    public async Task<ActionResult> GetGameStatus(int gameId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest("User not authenticated");

        var result = await gameService.GetGameStatusAsync(gameId, userId);
        return Ok(result);
    }
}
