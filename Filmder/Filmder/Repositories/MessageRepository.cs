using Filmder.Data;
using Filmder.Interfaces;
using Filmder.Models;
using Microsoft.EntityFrameworkCore;

namespace Filmder.Services;

public class MessageRepository(AppDbContext context) : IMessageRepository
{
    public async Task<List<object>> GetGroupMessagesAsync(int groupId, string userId)
    {
        var isMember = await context.GroupMembers
            .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId);

        if (!isMember)
            throw new UnauthorizedAccessException();

        return await context.Messages
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
                isOwn = m.UserId == userId
            })
            .Cast<object>()
            .ToListAsync();
    }

    public async Task<object> CreateMessageAsync(CreateMessageDto dto, string userId)
    {
        var isMember = await context.GroupMembers
            .AnyAsync(gm => gm.GroupId == dto.GroupId && gm.UserId == userId);

        if (!isMember)
            throw new UnauthorizedAccessException();

        var message = new Message
        {
            GroupId = dto.GroupId,
            UserId = userId,
            Content = dto.Content ?? dto.Message,
            SentAt = DateTime.UtcNow
        };

        context.Messages.Add(message);
        await context.SaveChangesAsync();

        var user = await context.Users.FindAsync(userId);

        return new
        {
            id = message.Id,
            content = message.Content,
            userName = user?.UserName ?? user?.Email ?? "Unknown",
            sentAt = message.SentAt,
            userId = message.UserId
        };
    }

    public async Task DeleteMessageAsync(int messageId, string userId)
    {
        var message = await context.Messages
            .Include(m => m.Group)
            .FirstOrDefaultAsync(m => m.Id == messageId);

        if (message == null)
            throw new Exception("Message not found");

        var isOwner = message.Group.OwnerId == userId;
        var isAuthor = message.UserId == userId;

        if (!isOwner && !isAuthor)
            throw new UnauthorizedAccessException();

        context.Messages.Remove(message);
        await context.SaveChangesAsync();
    }
}
