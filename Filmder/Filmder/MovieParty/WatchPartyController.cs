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
        if (userId == null) return Unauthorized();

        var isMember = await _context.GroupMembers
            .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId);

        if (!isMember)
            return Forbid("You must be a member of this group to create a watch party.");

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

        var result = await GetWatchPartyDto(watchParty.Id);
        return CreatedAtAction(nameof(GetWatchParty), new { id = watchParty.Id }, result);
    }

    [HttpGet("group/{groupId}")]
    public async Task<ActionResult<List<WatchPartyDto>>> GetGroupWatchParties(int groupId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var isMember = await _context.GroupMembers
            .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId);

        if (!isMember)
            return Forbid("You must be a member of this group to view watch parties.");

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

    [HttpGet("{id}")]
    public async Task<ActionResult<WatchPartyDto>> GetWatchParty(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var party = await _context.WatchParties
            .Include(wp => wp.Group)
            .FirstOrDefaultAsync(wp => wp.Id == id);

        if (party == null) return NotFound();

        var isMember = await _context.GroupMembers
            .AnyAsync(gm => gm.GroupId == party.GroupId && gm.UserId == userId);

        if (!isMember)
            return Forbid("You must be a member of this group to view this watch party.");

        var result = await GetWatchPartyDto(id);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteWatchParty(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var party = await _context.WatchParties
            .Include(wp => wp.Group)
            .FirstOrDefaultAsync(wp => wp.Id == id);

        if (party == null) return NotFound();

        var isHost = party.HostUserId == userId;
        var isAdmin = await _context.GroupMembers
            .AnyAsync(gm => gm.GroupId == party.GroupId && gm.UserId == userId);

        if (!isHost && !isAdmin)
            return Forbid("Only the host or group admin can delete this watch party.");

        _context.WatchParties.Remove(party);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id}/join")]
    public async Task<ActionResult<WatchPartyDto>> JoinWatchParty(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var party = await _context.WatchParties
            .Include(wp => wp.Group)
            .FirstOrDefaultAsync(wp => wp.Id == id);

        if (party == null) return NotFound();

        var isMember = await _context.GroupMembers
            .AnyAsync(gm => gm.GroupId == party.GroupId && gm.UserId == userId);

        if (!isMember)
            return Forbid("You must be a member of this group to join this watch party.");

        var now = DateTime.UtcNow;
        var timeDiff = (party.ScheduledTime - now).TotalMinutes;

        if (timeDiff > 15 && party.Status != WatchPartyStatus.Active)
            return BadRequest("This watch party hasn't started yet. You can join 15 minutes before the scheduled time.");

        if (party.Status == WatchPartyStatus.Scheduled && timeDiff <= 15)
        {
            party.Status = WatchPartyStatus.Active;
            await _context.SaveChangesAsync();
        }

        var result = await GetWatchPartyDto(id);
        return Ok(result);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateWatchPartyStatus(int id, [FromBody] WatchPartyStatus status)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var party = await _context.WatchParties.FindAsync(id);
        if (party == null) return NotFound();

        if (party.HostUserId != userId)
            return Forbid("Only the host can update the party status.");

        party.Status = status;
        await _context.SaveChangesAsync();

        return NoContent();
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