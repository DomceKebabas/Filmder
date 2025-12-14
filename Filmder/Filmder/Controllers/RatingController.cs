using Filmder.DTOs;
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
public class RatingController : ControllerBase
{
    private readonly IRatingService _ratingService;

    public RatingController(IRatingService ratingService)
    {
        _ratingService = ratingService;
    }

    [HttpPost("rate")]
    public async Task<IActionResult> RateMovie([FromBody] RateMovieDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var (success, message) = await _ratingService.RateMovieAsync(userId, dto);

        if (!success)
        {
            return NotFound(message);
        }

        return Ok(new { message });
    }

    [HttpGet("movie/{movieId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetRatings(int movieId)
    {
        var ratings = await _ratingService.GetRatingsByMovieAsync(movieId);
        return Ok(ratings);
    }

    [HttpGet("movie/{movieId}/average")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAverageRating(int movieId)
    {
        var result = await _ratingService.GetAverageRatingAsync(movieId);
        return Ok(new { averageScore = result.AverageScore, totalRatings = result.TotalRatings });
    }
}