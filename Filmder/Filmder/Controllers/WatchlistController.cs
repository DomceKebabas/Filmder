using System.Security.Claims;
using Filmder.DTOs;
using Filmder.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Filmder.Controllers;

[EnableRateLimiting("DefaultBucket")]
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WatchlistController : ControllerBase
{
    private readonly IWatchlistService _watchlistService;

    public WatchlistController(IWatchlistService watchlistService)
    {
        _watchlistService = watchlistService;
    }

    [HttpGet("generate")]
    public async Task<ActionResult<List<WatchlistMovieDto>>> GenerateWatchlist([FromQuery] int count = 20)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var watchlist = await _watchlistService.GenerateWatchlistAsync(userId, count);
        return Ok(watchlist);
    }

    [HttpGet("preferences")]
    public async Task<ActionResult<UserPreferencesDto>> GetUserPreferences()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var preferences = await _watchlistService.GetUserPreferencesAsync(userId);
        return Ok(preferences);
    }
}