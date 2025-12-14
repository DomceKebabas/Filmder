using Filmder.Data;
using Filmder.DTOs;
using Filmder.Interfaces;
using Filmder.Models;
using Microsoft.EntityFrameworkCore;

namespace Filmder.Services;

public class GroupRepository(AppDbContext context) : IGroupRepository
{
    public async Task<object> CreateGroupAsync(CreateGroupDto dto, string userId)
    {
        var user = await context.Users.FindAsync(userId);
        if (user == null) throw new UnauthorizedAccessException();

        var group = new Group
        {
            Name = dto.Name,
            OwnerId = userId
        };

        context.Groups.Add(group);
        await context.SaveChangesAsync();

        var groupMembers = new List<GroupMember>
        {
            new()
            {
                UserId = userId,
                GroupId = group.Id,
                JoinedAt = DateTime.UtcNow
            }
        };

        if (dto.FriendEmails != null && dto.FriendEmails.Any())
        {
            var friends = await context.Users
                .Where(u => dto.FriendEmails.Contains(u.Email))
                .ToListAsync();

            foreach (var friend in friends)
            {
                if (friend.Id != userId)
                {
                    groupMembers.Add(new GroupMember
                    {
                        UserId = friend.Id,
                        GroupId = group.Id,
                        JoinedAt = DateTime.UtcNow
                    });
                }
            }
        }

        context.GroupMembers.AddRange(groupMembers);
        await context.SaveChangesAsync();

        return await context.Groups
            .Where(g => context.GroupMembers.Any(m => m.GroupId == g.Id && m.UserId == userId))
            .Select(g => new { g.Id, g.Name, g.OwnerId })
            .ToListAsync();
    }

    public async Task<object> GetMyGroupsAsync(string userId)
    {
        return await context.Groups
            .Where(g => context.GroupMembers.Any(m => m.GroupId == g.Id && m.UserId == userId))
            .Select(g => new
            {
                g.Id,
                g.Name,
                g.OwnerId,
                MemberCount = g.GroupMembers.Count,
                ActiveGamesCount =
                    context.Games.Count(game => game.GroupId == g.Id && game.IsActive)
                    + context.RatingGuessingGames.Count(rg => rg.GroupId == g.Id && rg.IsActive),
                TotalGamesCount =
                    context.Games.Count(game => game.GroupId == g.Id)
                    + context.RatingGuessingGames.Count(rg => rg.GroupId == g.Id)
            })
            .ToListAsync();
    }

    public async Task AddToSharedMovieListAsync(int groupId, int movieId, string comment, string userId)
    {
        var group = await context.Groups
            .Include(g => g.GroupMembers)
            .Include(g => g.GroupMovie)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null) throw new Exception();

        var userIsInGroup = group.GroupMembers.Any(m => m.UserId == userId);
        if (!userIsInGroup) throw new UnauthorizedAccessException();

        var sharedMovie = new SharedMovie
        {
            GroupId = groupId,
            MovieId = movieId,
            UserWhoAddedId = userId,
            UserId = userId,
            Comment = comment
        };

        group.GroupMovie.Add(sharedMovie);
        await context.SaveChangesAsync();
    }

    public async Task<object> GetGroupByIdAsync(int groupId, string userId)
    {
        var group = await context.Groups
            .Where(g => g.Id == groupId && context.GroupMembers
                .Any(m => m.GroupId == g.Id && m.UserId == userId))
            .Select(g => new
            {
                g.Id,
                g.Name,
                g.OwnerId,
                MemberCount = g.GroupMembers.Count,
                Members = g.GroupMembers.Select(m => new
                {
                    m.UserId,
                    m.User.UserName,
                    m.User.Email,
                    m.User.ProfilePictureUrl,
                    m.JoinedAt
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (group == null) throw new Exception();
        return group;
    }

    public async Task<object> GetSharedMoviesAsync(int groupId, string userId)
    {
        var isMember = await context.GroupMembers
            .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId);

        if (!isMember) throw new UnauthorizedAccessException();

        return await context.SharedMovies
            .Where(sm => sm.GroupId == groupId)
            .Include(sm => sm.Movie)
            .Select(sm => new
            {
                sm.Id,
                sm.MovieId,
                sm.Comment,
                sm.AddedAt,
                AddedBy = sm.UserId,
                Movie = new
                {
                    sm.Movie.Id,
                    sm.Movie.Name,
                    sm.Movie.Genre,
                    sm.Movie.ReleaseYear,
                    sm.Movie.Rating,
                    sm.Movie.PosterUrl,
                    sm.Movie.Duration
                }
            })
            .ToListAsync();
    }

    public async Task AddMemberAsync(int groupId, string email, string requesterId)
    {
        var group = await context.Groups
            .Include(g => g.GroupMembers)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null) throw new Exception();
        if (!group.GroupMembers.Any(m => m.UserId == requesterId))
            throw new UnauthorizedAccessException();

        var userToAdd = await context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (userToAdd == null) throw new Exception();

        if (group.GroupMembers.Any(m => m.UserId == userToAdd.Id))
            throw new Exception();

        context.GroupMembers.Add(new GroupMember
        {
            GroupId = groupId,
            UserId = userToAdd.Id,
            JoinedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
    }

    public async Task KickMemberAsync(int groupId, string userId, string requesterId)
    {
        var group = await context.Groups
            .Include(g => g.GroupMembers)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null) throw new Exception();
        if (group.OwnerId != requesterId) throw new UnauthorizedAccessException();
        if (userId == group.OwnerId) throw new Exception();

        var member = group.GroupMembers.FirstOrDefault(m => m.UserId == userId);
        if (member == null) throw new Exception();

        context.GroupMembers.Remove(member);
        await context.SaveChangesAsync();
    }

    public async Task DeleteGroupAsync(int groupId, string userId)
    {
        var group = await context.Groups
            .Include(g => g.GroupMembers)
            .Include(g => g.Messages)
            .Include(g => g.GroupMovie)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null) throw new Exception();
        if (group.OwnerId != userId) throw new UnauthorizedAccessException();

        context.GroupMembers.RemoveRange(group.GroupMembers);
        context.SharedMovies.RemoveRange(group.GroupMovie);
        context.Messages.RemoveRange(group.Messages);
        context.Groups.Remove(group);

        await context.SaveChangesAsync();
    }

    public async Task<string> LeaveGroupAsync(int groupId, string userId)
    {
        var group = await context.Groups
            .Include(g => g.GroupMembers)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null) throw new Exception();

        var member = group.GroupMembers.FirstOrDefault(m => m.UserId == userId);
        if (member == null) throw new Exception();

        if (group.OwnerId == userId)
        {
            var others = group.GroupMembers.Where(m => m.UserId != userId).ToList();

            if (!others.Any())
            {
                context.GroupMembers.Remove(member);
                context.Groups.Remove(group);
                await context.SaveChangesAsync();
                return "Group deleted because owner left and no members remained.";
            }

            var newOwner = others[new Random().Next(others.Count)];
            group.OwnerId = newOwner.UserId;
        }

        context.GroupMembers.Remove(member);
        await context.SaveChangesAsync();
        return "Left group.";
    }
}
