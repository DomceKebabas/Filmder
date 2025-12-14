using Filmder.DTOs.HigherLower;
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
public class HigherLowerController(IHigherLowerService service) : ControllerBase
{
    [HttpPost("start")]
    public async Task<ActionResult<StartGameResponseDto>> StartGame()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var result = await service.StartGameAsync(userId);
        return Ok(result);
    }

    [HttpPost("guess")]
    public async Task<ActionResult<GuessResultDto>> SubmitGuess(HigherLowerGuessDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var result = await service.SubmitGuessAsync(dto, userId);
        return Ok(result);
    }

    [HttpGet("stats")]
    public async Task<ActionResult<HigherLowerStatsDto>> GetMyStats()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var result = await service.GetMyStatsAsync(userId);
        return Ok(result);
    }

    [HttpGet("leaderboard")]
    public async Task<ActionResult<List<LeaderboardEntryDto>>> GetLeaderboard([FromQuery] int limit = 10)
    {
        var result = await service.GetLeaderboardAsync(limit);
        return Ok(result);
    }

    [HttpDelete("end-game/{gameId}")]
    public async Task<ActionResult> EndGame(int gameId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var result = await service.EndGameAsync(gameId, userId);
        return Ok(result);
    }
}