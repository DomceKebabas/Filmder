using Filmder.DTOs;
using Filmder.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Filmder.Controllers;

[EnableRateLimiting("DefaultBucket")]
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MoodController(IMoodService moodService) : ControllerBase
{
    [HttpPost("recommend")]
    [AllowAnonymous]
    public async Task<ActionResult<MoodMovieResponseDto>> GetMovieByMood([FromBody] MoodDto moodDto)
    {
        var result = await moodService.GetMovieByMoodAsync(moodDto);
        return Ok(result);
    }

    [HttpGet("moods")]
    [AllowAnonymous]
    public ActionResult<List<object>> GetAvailableMoods()
    {
        var result = moodService.GetAvailableMoods();
        return Ok(result);
    }
}