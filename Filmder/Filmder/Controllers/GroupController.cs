using Filmder.Data;
using Filmder.DTOs;
using Filmder.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.RateLimiting;

namespace Filmder.Controllers;
[EnableRateLimiting("DefaultBucket")]
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GroupController(AppDbContext context) : ControllerBase
{
    [HttpPost("create")]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var user = await context.Users.FindAsync(userId);
        if (user == null) return Unauthorized();

        var group = new Group
        {
            Name = dto.Name,
            OwnerId = userId
        };

        context.Groups.Add(group);
        await context.SaveChangesAsync(); 

        var groupMembers = new List<GroupMember>
        {
            new GroupMember
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

        var groups = await context.Groups
            .Where(g => context.GroupMembers
                .Any(m => m.GroupId == g.Id && m.UserId == userId))
            .Select(g => new { g.Id, g.Name, g.OwnerId })
            .ToListAsync();

        return Ok(groups);
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMyGroups()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var groups = await context.Groups
            .Where(g => context.GroupMembers
                .Any(m => m.GroupId == g.Id && m.UserId == userId))
            .Select(g => new 
            { 
                g.Id, 
                g.Name, 
                g.OwnerId,
                MemberCount = g.GroupMembers.Count,
                ActiveGamesCount = context.Games.Count(game => game.GroupId == g.Id && game.IsActive)
                    + context.RatingGuessingGames.Count(rg => rg.GroupId == g.Id && rg.IsActive),
                TotalGamesCount = context.Games.Count(game => game.GroupId == g.Id)
                    + context.RatingGuessingGames.Count(rg => rg.GroupId == g.Id)
            })
            .ToListAsync();

        return Ok(groups);
    }
    
    [HttpPost("groups/{groupId}/shared-movies/{movieId}")]
    public async Task<IActionResult> AddToSharedMovieList(int groupId, int movieId, [FromBody] string comment)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest();

        var group = await context.Groups
            .Include(g => g.GroupMembers)
            .Include(g => g.GroupMovie)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null) return BadRequest();

        
        var userIsInGroup= group.GroupMembers.Any(s => s.UserId == userId && s.GroupId == groupId);
        if (!userIsInGroup) return Forbid();

        var isAlreadyAdded = group.GroupMovie.Any(gm => gm.MovieId == movieId);
        

        var sharedMovie = new SharedMovie
        {
            GroupId = groupId,
            MovieId = movieId,
            UserWhoAddedId = userId,
            UserId = userId, //temp fix
            Comment = comment
        };
        
        group.GroupMovie.Add(sharedMovie);
        await context.SaveChangesAsync();

        return Ok();

    }
    
    
    
    [HttpGet("{groupId}")]
    public async Task<IActionResult> GetGroupById(int groupId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

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

        if (group == null) return NotFound();

        return Ok(group);
    }
    
    [HttpGet("{groupId}/shared-movies")]
    public async Task<IActionResult> GetSharedMovies(int groupId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest();

        var isMember = await context.GroupMembers
            .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId);

        if (!isMember) return Forbid();

        var sharedMovies = await context.SharedMovies
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

        return Ok(sharedMovies);
    }
    
    [HttpPost("{groupId}/add-member")]
    public async Task<IActionResult> AddMember(int groupId, [FromBody] string email)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var group = await context.Groups
            .Include(g => g.GroupMembers)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null) return NotFound();

        var isMember = group.GroupMembers.Any(m => m.UserId == userId);
        if (!isMember) return Forbid(); 

        var userToAdd = await context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (userToAdd == null) return NotFound("User not found");

        var alreadyInGroup = group.GroupMembers.Any(m => m.UserId == userToAdd.Id);
        if (alreadyInGroup) return BadRequest("User already in group");

        context.GroupMembers.Add(new GroupMember
        {
            GroupId = groupId,
            UserId = userToAdd.Id,
            JoinedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
        return Ok("User added");
    }
    [HttpDelete("{groupId}/kick/{userId}")]
    public async Task<IActionResult> KickMember(int groupId, string userId)
    {
        var requesterId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var group = await context.Groups
            .Include(g => g.GroupMembers)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null) return NotFound();
        if (group.OwnerId != requesterId) return Forbid();

        var member = group.GroupMembers.FirstOrDefault(m => m.UserId == userId);
        if (member == null) return NotFound("User is not in group");

        if (userId == group.OwnerId) return BadRequest("Owner cannot be removed");

        context.GroupMembers.Remove(member);
        await context.SaveChangesAsync();

        return Ok("User removed");
    }

    
    [HttpDelete("{groupId}")]
    public async Task<IActionResult> DeleteGroup(int groupId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var group = await context.Groups
            .Include(g => g.GroupMembers)
            .Include(g => g.Messages)
            .Include(g => g.GroupMovie)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null) return NotFound();
        if (group.OwnerId != userId) return Forbid();

        context.GroupMembers.RemoveRange(group.GroupMembers);
        context.SharedMovies.RemoveRange(group.GroupMovie);
        context.Messages.RemoveRange(group.Messages);
        context.Groups.Remove(group);

        await context.SaveChangesAsync();

        return Ok("Group deleted");
    }

    
    [HttpDelete("{groupId}/leave")]
    public async Task<IActionResult> LeaveGroup(int groupId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var group = await context.Groups
            .Include(g => g.GroupMembers)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null) return NotFound();

        var member = group.GroupMembers.FirstOrDefault(m => m.UserId == userId);
        if (member == null) return BadRequest("Not in group");

        bool isOwner = group.OwnerId == userId;

        if (isOwner)
        {
            var otherMembers = group.GroupMembers
                .Where(m => m.UserId != userId)
                .ToList();

            if (!otherMembers.Any())
            {
                context.GroupMembers.Remove(member);
                context.Groups.Remove(group);
                await context.SaveChangesAsync();

                return Ok("Group deleted because owner left and no members remained.");
            }

            var randomGenerator = new Random();
            var randomIndex = randomGenerator.Next(otherMembers.Count);
            var newOwner = otherMembers[randomIndex];

            group.OwnerId = newOwner.UserId;
        }

        context.GroupMembers.Remove(member);
        await context.SaveChangesAsync();

        return Ok("Left group.");
    }

}