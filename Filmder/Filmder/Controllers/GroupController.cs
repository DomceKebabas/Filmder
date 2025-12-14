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
public class GroupController(IGroupService groupService) : ControllerBase
{
    [HttpPost("create")]
    public async Task<IActionResult> CreateGroup(CreateGroupDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await groupService.CreateGroupAsync(dto, userId);
        return Ok(result);
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMyGroups()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await groupService.GetMyGroupsAsync(userId);
        return Ok(result);
    }

    [HttpPost("groups/{groupId}/shared-movies/{movieId}")]
    public async Task<IActionResult> AddToSharedMovieList(int groupId, int movieId, [FromBody] string comment)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest();

        await groupService.AddToSharedMovieListAsync(groupId, movieId, comment, userId);
        return Ok();
    }

    [HttpGet("{groupId}")]
    public async Task<IActionResult> GetGroupById(int groupId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await groupService.GetGroupByIdAsync(groupId, userId);
        return Ok(result);
    }

    [HttpGet("{groupId}/shared-movies")]
    public async Task<IActionResult> GetSharedMovies(int groupId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest();

        var result = await groupService.GetSharedMoviesAsync(groupId, userId);
        return Ok(result);
    }

    [HttpPost("{groupId}/add-member")]
    public async Task<IActionResult> AddMember(int groupId, [FromBody] string email)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await groupService.AddMemberAsync(groupId, email, userId);
        return Ok("User added");
    }

    [HttpDelete("{groupId}/kick/{userId}")]
    public async Task<IActionResult> KickMember(int groupId, string userId)
    {
        var requesterId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await groupService.KickMemberAsync(groupId, userId, requesterId);
        return Ok("User removed");
    }

    [HttpDelete("{groupId}")]
    public async Task<IActionResult> DeleteGroup(int groupId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await groupService.DeleteGroupAsync(groupId, userId);
        return Ok("Group deleted");
    }

    [HttpDelete("{groupId}/leave")]
    public async Task<IActionResult> LeaveGroup(int groupId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await groupService.LeaveGroupAsync(groupId, userId);
        return Ok(result);
    }
}
