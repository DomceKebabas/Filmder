using System.Security.Claims;
using Filmder.DTOs;
using Filmder.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Filmder.Controllers;

[EnableRateLimiting("ExpensiveDaily")]
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PersonalizedPlaylistController : ControllerBase
{
    private readonly IPersonalizedPlaylistService _playlistService;

    public PersonalizedPlaylistController(IPersonalizedPlaylistService playlistService)
    {
        _playlistService = playlistService;
    }

    [HttpGet("generate")]
    public async Task<ActionResult<PersonalizedPlaylistDto>> GeneratePlaylist([FromQuery] int count = 10)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        if (count < 5 || count > 30)
        {
            return BadRequest(new { message = "Count must be between 5 and 30" });
        }

        var result = await _playlistService.GeneratePlaylistAsync(userId, count);

        if (!result.Success)
        {
            return result.StatusCode switch
            {
                404 => NotFound(new { message = result.ErrorMessage }),
                500 => StatusCode(500, new { message = "Failed to generate playlist", details = result.ErrorMessage }),
                _ => BadRequest(new { message = result.ErrorMessage })
            };
        }

        return Ok(result.Playlist);
    }

    [HttpGet("quick-picks")]
    public async Task<ActionResult> GetQuickPicks()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var result = await _playlistService.GetQuickPicksAsync(userId);

        if (!result.Success)
        {
            return result.StatusCode switch
            {
                404 => NotFound(new { message = result.ErrorMessage }),
                500 => StatusCode(500, new { message = "Failed to get quick picks", details = result.ErrorMessage }),
                _ => BadRequest(new { message = result.ErrorMessage })
            };
        }

        return Ok(new
        {
            title = result.Title,
            description = result.Description,
            movies = result.Movies
        });
    }
}