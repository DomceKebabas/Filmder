using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Filmder.Data;
using System.Security.Claims;
using System.Collections.Concurrent;

namespace Filmder.Signal;

[Authorize]
public class WatchPartyHub : Hub
{
    private readonly AppDbContext _context;
    private readonly ILogger<WatchPartyHub> _logger;
    
    private static readonly ConcurrentDictionary<int, ConcurrentDictionary<string, string>> _partyConnections = new();

    public WatchPartyHub(AppDbContext context, ILogger<WatchPartyHub> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task JoinParty(int partyId, string userName)
    {
        try
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized join attempt for party {PartyId}", partyId);
                throw new HubException("Unauthorized: You must be logged in to join a watch party.");
            }

            var party = await _context.WatchParties
                .Include(wp => wp.Group)
                .FirstOrDefaultAsync(wp => wp.Id == partyId);

            if (party == null)
            {
                _logger.LogWarning("Party {PartyId} not found", partyId);
                throw new HubException("Watch party not found.");
            }

            var isMember = await _context.GroupMembers
                .AnyAsync(gm => gm.GroupId == party.GroupId && gm.UserId == userId);

            if (!isMember)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to join party {PartyId} but is not a member of group {GroupId}",
                    userId, partyId, party.GroupId);
                throw new HubException("Access denied: You must be a member of this group to join this watch party.");
            }

            var now = DateTime.UtcNow;
            var timeDiff = (party.ScheduledTime - now).TotalMinutes;

            if (party.Status == MovieParty.WatchPartyStatus.Completed)
            {
                throw new HubException("This watch party has already ended.");
            }

            if (timeDiff > 15 && party.Status != MovieParty.WatchPartyStatus.Active)
            {
                throw new HubException($"This watch party hasn't started yet. You can join {Math.Ceiling(timeDiff - 15)} minutes before the scheduled time.");
            }

            if (party.Status == MovieParty.WatchPartyStatus.Scheduled && timeDiff <= 15)
            {
                party.Status = MovieParty.WatchPartyStatus.Active;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Party {PartyId} auto-activated", partyId);
            }

            var groupName = $"party-{partyId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            var partyConnections = _partyConnections.GetOrAdd(partyId, _ => new ConcurrentDictionary<string, string>());
            partyConnections.TryAdd(Context.ConnectionId, userName);

            _logger.LogInformation(
                "User {UserId} ({UserName}) joined party {PartyId}. Total participants: {Count}",
                userId, userName, partyId, partyConnections.Count);

            await Clients.Group(groupName).SendAsync("ParticipantCountUpdated", partyConnections.Count);
            await Clients.OthersInGroup(groupName).SendAsync("UserJoined", userName);
            
            var participants = partyConnections.Values.ToList();
            await Clients.Caller.SendAsync("ParticipantList", participants);
        }
        catch (HubException)
        {
            throw; 
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in JoinParty for party {PartyId}", partyId);
            throw new HubException("An error occurred while joining the watch party. Please try again.");
        }
    }

    public async Task SendPause(int partyId)
    {
        try
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                throw new HubException("Unauthorized");
            }

            await VerifyPartyAccess(partyId, userId);

            var groupName = $"party-{partyId}";
            _logger.LogDebug("User {UserId} paused party {PartyId}", userId, partyId);
            await Clients.OthersInGroup(groupName).SendAsync("ReceivePause");
        }
        catch (HubException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SendPause for party {PartyId}", partyId);
            throw new HubException("Failed to send pause command.");
        }
    }
    
    public async Task SendPlay(int partyId, double time)
    {
        try
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                throw new HubException("Unauthorized");
            }

            await VerifyPartyAccess(partyId, userId);

            var groupName = $"party-{partyId}";
            _logger.LogDebug("User {UserId} played party {PartyId} at {Time}s", userId, partyId, time);
            await Clients.OthersInGroup(groupName).SendAsync("ReceivePlay", time);
        }
        catch (HubException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SendPlay for party {PartyId}", partyId);
            throw new HubException("Failed to send play command.");
        }
    }
    
    public async Task SendSeek(int partyId, double time)
    {
        try
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                throw new HubException("Unauthorized");
            }

            await VerifyPartyAccess(partyId, userId);

            var groupName = $"party-{partyId}";
            _logger.LogDebug("User {UserId} seeked party {PartyId} to {Time}s", userId, partyId, time);
            await Clients.OthersInGroup(groupName).SendAsync("ReceiveSeek", time);
        }
        catch (HubException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SendSeek for party {PartyId}", partyId);
            throw new HubException("Failed to send seek command.");
        }
    }
    
    public async Task SendChatMessage(int partyId, string message)
    {
        try
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                throw new HubException("Unauthorized");
            }

            // Get user's display name from database
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new HubException("User not found");
            }

            var userName = user.UserName ?? "Unknown User";

            await VerifyPartyAccess(partyId, userId);

            // Validate message
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new HubException("Message cannot be empty");
            }

            if (message.Length > 500)
            {
                throw new HubException("Message is too long (max 500 characters)");
            }

            var groupName = $"party-{partyId}";
            _logger.LogDebug("User {UserId} sent chat to party {PartyId}", userId, partyId);
            
            await Clients.Group(groupName).SendAsync("ReceiveChatMessage", userName, message, DateTime.UtcNow);
        }
        catch (HubException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SendChatMessage for party {PartyId}", partyId);
            throw new HubException("Failed to send chat message.");
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            foreach (var partyKvp in _partyConnections)
            {
                if (partyKvp.Value.TryRemove(Context.ConnectionId, out var userName))
                {
                    var partyId = partyKvp.Key;
                    var groupName = $"party-{partyId}";
                    
                    _logger.LogInformation(
                        "User {UserName} disconnected from party {PartyId}. Remaining: {Count}",
                        userName, partyId, partyKvp.Value.Count);

                    await Clients.Group(groupName).SendAsync("UserLeft", userName);
                    await Clients.Group(groupName).SendAsync("ParticipantCountUpdated", partyKvp.Value.Count);

                    if (partyKvp.Value.IsEmpty)
                    {
                        _partyConnections.TryRemove(partyId, out _);
                        _logger.LogInformation("Party {PartyId} is now empty", partyId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OnDisconnectedAsync");
        }

        await base.OnDisconnectedAsync(exception);
    }

   
    private async Task VerifyPartyAccess(int partyId, string userId)
    {
        var party = await _context.WatchParties
            .AsNoTracking()
            .FirstOrDefaultAsync(wp => wp.Id == partyId);

        if (party == null)
        {
            throw new HubException("Watch party not found");
        }

        var isMember = await _context.GroupMembers
            .AsNoTracking()
            .AnyAsync(gm => gm.GroupId == party.GroupId && gm.UserId == userId);

        if (!isMember)
        {
            _logger.LogWarning(
                "User {UserId} attempted action on party {PartyId} without group membership",
                userId, partyId);
            throw new HubException("Access denied: You are not a member of this watch party's group");
        }
    }

   
    public Task<int> GetParticipantCount(int partyId)
    {
        if (_partyConnections.TryGetValue(partyId, out var connections))
        {
            return Task.FromResult(connections.Count);
        }
        return Task.FromResult(0);
    }
}