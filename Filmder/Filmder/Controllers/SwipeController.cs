using Filmder.DTOs;
using Filmder.Models;
using Filmder.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace Filmder.Controllers;

[EnableRateLimiting("DefaultBucket")]
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SwipeController : ControllerBase
{
    private readonly ISwipeService _swipeService;

    public SwipeController(ISwipeService swipeService)
    {
        _swipeService = swipeService;
    }

    [HttpGet("random")]
    public async Task<ActionResult<Movie>> GetRandomMovie(
        [FromQuery] string? genre = null,
        [FromQuery] int? minYear = null,
        [FromQuery] int? maxDuration = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var (movie, errorMessage) = await _swipeService.GetRandomMovieAsync(userId, genre, minYear, maxDuration);

        if (movie == null)
        {
            return NotFound(new { message = errorMessage });
        }

        return Ok(movie);
    }

    [HttpPost("swipe")]
    public async Task<ActionResult> RecordSwipe([FromBody] SwipeDto swipeDto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var (success, message) = await _swipeService.RecordSwipeAsync(userId, swipeDto);

        if (!success)
        {
            if (message == "Movie not found")
                return NotFound(new { message });
            return BadRequest(new { message });
        }

        return Ok(new { message });
    }

    [HttpGet("history")]
    public async Task<ActionResult<List<SwipeHistoryDto>>> GetSwipeHistory(
        [FromQuery] bool? onlyLikes = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var history = await _swipeService.GetSwipeHistoryAsync(userId, onlyLikes, page, pageSize);

        return Ok(history);
    }

    [HttpGet("liked")]
    public async Task<ActionResult<List<Movie>>> GetLikedMovies(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var likedMovies = await _swipeService.GetLikedMoviesAsync(userId, page, pageSize);

        return Ok(likedMovies);
    }

    [HttpDelete("history/{swipeId}")]
    public async Task<ActionResult> DeleteSwipe(int swipeId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var (success, message) = await _swipeService.DeleteSwipeAsync(userId, swipeId);

        if (!success)
        {
            return NotFound(new { message });
        }

        return Ok(new { message });
    }

    [HttpGet("stats")]
    public async Task<ActionResult> GetSwipeStats()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var stats = await _swipeService.GetSwipeStatsAsync(userId);

        return Ok(new
        {
            totalSwipes = stats.TotalSwipes,
            totalLikes = stats.TotalLikes,
            totalDislikes = stats.TotalDislikes,
            likePercentage = stats.LikePercentage,
            favoriteGenre = stats.FavoriteGenre
        });
    }
}