using System.Security.Claims;
using Filmder.Data;
using Filmder.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Filmder.Controllers;

[EnableRateLimiting("DefaultBucket")]
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MessageController : ControllerBase
{
    private readonly AppDbContext _context;

    public MessageController(AppDbContext context)
    {
        _context = context;
    }

    // Get messages for a group
    [HttpGet("{groupId}")]
    public async Task<IActionResult> GetGroupMessages(int groupId)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == null) return Unauthorized();

        // Verify user is a member of the group
        var isMember = await _context.GroupMembers
            .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == currentUserId);

        if (!isMember) return Forbid();

        var messages = await _context.Messages
            .Where(m => m.GroupId == groupId)
            .Include(m => m.User)
            .OrderBy(m => m.SentAt)
            .Select(m => new
            {
                id = m.Id,
                content = m.Content,
                userName = m.User.UserName ?? m.User.Email,
                sentAt = m.SentAt,
                userId = m.UserId,
                isOwn = m.UserId == currentUserId // Add this to help frontend
            })
            .ToListAsync();

        return Ok(messages);
    }

    // Save a message
    [HttpPost]
    public async Task<IActionResult> CreateMessage([FromBody] CreateMessageDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        // Verify user is a member of the group
        var isMember = await _context.GroupMembers
            .AnyAsync(gm => gm.GroupId == dto.GroupId && gm.UserId == userId);

        if (!isMember) return Forbid();

        var message = new Message
        {
            GroupId = dto.GroupId,
            UserId = userId,
            Content = dto.Content ?? dto.Message, // Support both field names
            SentAt = DateTime.UtcNow
        };

        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        var user = await _context.Users.FindAsync(userId);
        
        return Ok(new
        {
            id = message.Id,
            content = message.Content,
            userName = user?.UserName ?? user?.Email ?? "Unknown",
            sentAt = message.SentAt,
            userId = message.UserId
        });
    }

    // Delete a message (optional - for cleanup)
    [HttpDelete("{messageId}")]
    public async Task<IActionResult> DeleteMessage(int messageId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var message = await _context.Messages
            .Include(m => m.Group)
            .FirstOrDefaultAsync(m => m.Id == messageId);

        if (message == null) return NotFound();

        // Only allow deletion by message author or group owner
        var isOwner = message.Group.OwnerId == userId;
        var isAuthor = message.UserId == userId;

        if (!isOwner && !isAuthor) return Forbid();

        _context.Messages.Remove(message);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public class CreateMessageDto
{
    public int GroupId { get; set; }
    public string? Content { get; set; }
    public string? Message { get; set; } // Alternative field name for compatibility
}