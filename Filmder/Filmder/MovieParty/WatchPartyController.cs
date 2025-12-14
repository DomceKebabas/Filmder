using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Filmder.Data;
using Filmder.DTOs;
using Filmder.Models;
using System.Security.Claims;
using Filmder.MovieParty;

namespace Filmder.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WatchPartyController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<WatchPartyController> _logger;

    public WatchPartyController(AppDbContext context, ILogger<WatchPartyController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpPost("create/{groupId}")]
    public async Task<ActionResult<WatchPartyDto>> CreateWatchParty(int groupId, [FromBody] CreateWatchPartyDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) 
        {
            _logger.LogWarning("Unauthorized watch party creation attempt");
            return Unauthorized();
        }

        var isMember = await _context.GroupMembers
            .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId);

        if (!isMember)
        {
            _logger.LogWarning("User {UserId} attempted to create watch party for group {GroupId} without membership", userId, groupId);
            return Forbid("You must be a member of this group to create a watch party.");
        }

        if (dto.ScheduledTime <= DateTime.UtcNow)
        {
            return BadRequest("Scheduled time must be in the future.");
        }

        if (string.IsNullOrWhiteSpace(dto.MovieTitle))
        {
            return BadRequest("Movie title is required.");
        }

        try
        {
            var watchParty = new WatchParty
            {
                GroupId = groupId,
                HostUserId = userId,
                MovieTitle = dto.MovieTitle,
                Platform = dto.Platform,
                ScheduledTime = dto.ScheduledTime,
                VideoUrl = dto.VideoUrl,
                Status = WatchPartyStatus.Scheduled,
                CreatedAt = DateTime.UtcNow
            };

            _context.WatchParties.Add(watchParty);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Watch party {PartyId} created by user {UserId} for group {GroupId}", watchParty.Id, userId, groupId);

            var result = await GetWatchPartyDto(watchParty.Id);
            return CreatedAtAction(nameof(GetWatchParty), new { id = watchParty.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating watch party for group {GroupId}", groupId);
            return StatusCode(500, "An error occurred while creating the watch party.");
        }
    }

    [HttpGet("group/{groupId}")]
    public async Task<ActionResult<List<WatchPartyDto>>> GetGroupWatchParties(int groupId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var isMember = await _context.GroupMembers
            .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId);

        if (!isMember)
        {
            _logger.LogWarning("User {UserId} attempted to view watch parties for group {GroupId} without membership", userId, groupId);
            return Forbid("You must be a member of this group to view watch parties.");
        }

        try
        {
            var parties = await _context.WatchParties
                .Include(wp => wp.Host)
                .Include(wp => wp.Group)
                .Where(wp => wp.GroupId == groupId)
                .OrderBy(wp => wp.ScheduledTime)
                .Select(wp => new WatchPartyDto
                {
                    Id = wp.Id,
                    MovieTitle = wp.MovieTitle,
                    Platform = wp.Platform,
                    ScheduledTime = wp.ScheduledTime,
                    Status = wp.Status,
                    VideoUrl = wp.VideoUrl,
                    HostName = wp.Host.UserName ?? "Unknown",
                    HostUserId = wp.HostUserId,
                    GroupName = wp.Group.Name,
                    GroupId = wp.GroupId,
                    CreatedAt = wp.CreatedAt
                })
                .ToListAsync();

            return Ok(parties);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving watch parties for group {GroupId}", groupId);
            return StatusCode(500, "An error occurred while retrieving watch parties.");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<WatchPartyDto>> GetWatchParty(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var party = await _context.WatchParties
            .Include(wp => wp.Group)
            .FirstOrDefaultAsync(wp => wp.Id == id);

        if (party == null) 
        {
            _logger.LogWarning("Watch party {PartyId} not found", id);
            return NotFound("Watch party not found.");
        }

        // CRITICAL: Verify user is a member of the group
        var isMember = await _context.GroupMembers
            .AnyAsync(gm => gm.GroupId == party.GroupId && gm.UserId == userId);

        if (!isMember)
        {
            _logger.LogWarning("User {UserId} attempted to access watch party {PartyId} without group membership", userId, id);
            return Forbid("You must be a member of this group to view this watch party.");
        }

        try
        {
            var result = await GetWatchPartyDto(id);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving watch party {PartyId}", id);
            return StatusCode(500, "An error occurred while retrieving the watch party.");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteWatchParty(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var party = await _context.WatchParties
            .Include(wp => wp.Group)
            .FirstOrDefaultAsync(wp => wp.Id == id);

        if (party == null) 
        {
            _logger.LogWarning("Watch party {PartyId} not found for deletion", id);
            return NotFound("Watch party not found.");
        }

        var isHost = party.HostUserId == userId;
        
        var group = await _context.Groups.FindAsync(party.GroupId);
        var isGroupOwner = group?.OwnerId == userId;

        if (!isHost && !isGroupOwner)
        {
            _logger.LogWarning("User {UserId} attempted to delete watch party {PartyId} without permission", userId, id);
            return Forbid("Only the host or group owner can delete this watch party.");
        }

        try
        {
            _context.WatchParties.Remove(party);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Watch party {PartyId} deleted by user {UserId}", id, userId);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting watch party {PartyId}", id);
            return StatusCode(500, "An error occurred while deleting the watch party.");
        }
    }

    [HttpPost("{id}/join")]
    public async Task<ActionResult<WatchPartyDto>> JoinWatchParty(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var party = await _context.WatchParties
            .Include(wp => wp.Group)
            .FirstOrDefaultAsync(wp => wp.Id == id);

        if (party == null) 
        {
            _logger.LogWarning("Watch party {PartyId} not found for join", id);
            return NotFound("Watch party not found.");
        }

        // CRITICAL: Verify user is a member of the group
        var isMember = await _context.GroupMembers
            .AnyAsync(gm => gm.GroupId == party.GroupId && gm.UserId == userId);

        if (!isMember)
        {
            _logger.LogWarning("User {UserId} attempted to join watch party {PartyId} without group membership", userId, id);
            return Forbid("You must be a member of this group to join this watch party.");
        }

        var now = DateTime.UtcNow;
        var timeDiff = (party.ScheduledTime - now).TotalMinutes;

        if (party.Status == WatchPartyStatus.Completed)
        {
            return BadRequest("This watch party has already ended.");
        }

        if (timeDiff > 15 && party.Status != WatchPartyStatus.Active)
        {
            return BadRequest($"This watch party hasn't started yet. You can join {Math.Ceiling(timeDiff - 15)} minutes before the scheduled time.");
        }

        try
        {
            if (party.Status == WatchPartyStatus.Scheduled && timeDiff <= 15)
            {
                party.Status = WatchPartyStatus.Active;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Watch party {PartyId} auto-activated by user {UserId}", id, userId);
            }

            var result = await GetWatchPartyDto(id);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining watch party {PartyId}", id);
            return StatusCode(500, "An error occurred while joining the watch party.");
        }
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateWatchPartyStatus(int id, [FromBody] WatchPartyStatus status)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var party = await _context.WatchParties.FindAsync(id);
        if (party == null) 
        {
            _logger.LogWarning("Watch party {PartyId} not found for status update", id);
            return NotFound("Watch party not found.");
        }

        if (party.HostUserId != userId)
        {
            _logger.LogWarning("User {UserId} attempted to update status of watch party {PartyId} without being host", userId, id);
            return Forbid("Only the host can update the party status.");
        }

        try
        {
            party.Status = status;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Watch party {PartyId} status updated to {Status} by user {UserId}", id, status, userId);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating watch party {PartyId} status", id);
            return StatusCode(500, "An error occurred while updating the watch party status.");
        }
    }

 
    [HttpGet("{id}/extension-url")]
    public async Task<ActionResult<string>> GetExtensionUrl(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var party = await _context.WatchParties
            .Include(wp => wp.Group)
            .FirstOrDefaultAsync(wp => wp.Id == id);

        if (party == null) return NotFound("Watch party not found.");

        var isMember = await _context.GroupMembers
            .AnyAsync(gm => gm.GroupId == party.GroupId && gm.UserId == userId);

        if (!isMember)
        {
            return Forbid("You must be a member of this group to access this watch party.");
        }

        if (string.IsNullOrEmpty(party.VideoUrl))
        {
            return BadRequest("This watch party doesn't have a video URL set.");
        }

        var separator = party.VideoUrl.Contains('?') ? '&' : '?';
        var urlWithParty = $"{party.VideoUrl}{separator}filmder_party={id}";

        return Ok(urlWithParty);
    }

    private async Task<WatchPartyDto> GetWatchPartyDto(int id)
    {
        var party = await _context.WatchParties
            .Include(wp => wp.Host)
            .Include(wp => wp.Group)
            .FirstAsync(wp => wp.Id == id);

        return new WatchPartyDto
        {
            Id = party.Id,
            MovieTitle = party.MovieTitle,
            Platform = party.Platform,
            ScheduledTime = party.ScheduledTime,
            Status = party.Status,
            VideoUrl = party.VideoUrl,
            HostName = party.Host.UserName ?? "Unknown",
            HostUserId = party.HostUserId,
            GroupName = party.Group.Name,
            GroupId = party.GroupId,
            CreatedAt = party.CreatedAt
        };
    }
}