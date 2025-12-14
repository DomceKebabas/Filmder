using System.Security.Claims;
using Filmder.DTOs;
using Filmder.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Filmder.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("DefaultBucket")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("profile")]
    public async Task<ActionResult<UserProfileDto>> ReturnLoggedInUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest();

        var profile = await _userService.GetUserProfileAsync(userId);
        if (profile == null) return NotFound();

        return profile;
    }

    [HttpGet("stats")]
    public async Task<ActionResult<UserStatsDto>> GetLoggedInUserStatsAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest();

        var stats = await _userService.GetUserStatsAsync(userId);

        return stats;
    }

    [HttpPost("watch")]
    public async Task<IActionResult> AddMovieToUser(AddMovieRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest();

        var (success, message) = await _userService.AddMovieToUserAsync(userId, request);

        if (!success)
        {
            if (message == "Movie not found")
                return NotFound();
            return BadRequest(message);
        }

        if (message == null)
            return NoContent();

        return Ok();
    }

    [HttpPost("upload-profile-picture")]
    public async Task<IActionResult> UploadProfilePicture(IFormFile file)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var (success, message, url) = await _userService.UploadProfilePictureAsync(userId, file);

        if (!success)
        {
            if (message == "User not found")
                return NotFound();
            if (message == "Upload failed")
                return StatusCode(500, message);
            return BadRequest(message);
        }

        return Ok(new
        {
            message,
            profilePictureUrl = url
        });
    }

    [HttpDelete("profile-picture")]
    public async Task<IActionResult> DeleteProfilePicture()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var (success, message) = await _userService.DeleteProfilePictureAsync(userId);

        if (!success)
        {
            return NotFound();
        }

        return Ok(new { message });
    }
}