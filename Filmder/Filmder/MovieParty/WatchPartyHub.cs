using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Filmder.Data;

namespace Filmder.Signal;

public class WatchPartyHub : Hub
{
    private readonly AppDbContext _context;

    public WatchPartyHub(AppDbContext context)
    {
        _context = context;
    }

    public async Task JoinParty(int partyId, string userName, string userId)
    {
        var party = await _context.WatchParties
            .Include(wp => wp.Group)
            .FirstOrDefaultAsync(wp => wp.Id == partyId);

        if (party == null)
        {
            throw new HubException("Watch party not found.");
        }

        var isMember = await _context.GroupMembers
            .AnyAsync(gm => gm.GroupId == party.GroupId && gm.UserId == userId);

        if (!isMember)
        {
            throw new HubException("You must be a member of this group to join this watch party.");
        }

        var groupName = $"party-{partyId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        await Clients.OthersInGroup(groupName).SendAsync("UserJoined", userName);
    }

    public async Task SendPause(int partyId)
    {
        var groupName = $"party-{partyId}";
        await Clients.OthersInGroup(groupName).SendAsync("ReceivePause");
    }
    
    public async Task SendPlay(int partyId, double time)
    {
        var groupName = $"party-{partyId}";
        await Clients.OthersInGroup(groupName).SendAsync("ReceivePlay", time);
    }
    
    public async Task SendSeek(int partyId, double time)
    {
        var groupName = $"party-{partyId}";
        await Clients.OthersInGroup(groupName).SendAsync("ReceiveSeek", time);
    }
    
    public async Task SendChatMessage(int partyId, string userName, string message)
    {
        var groupName = $"party-{partyId}";
        await Clients.Group(groupName).SendAsync("ReceiveChatMessage", userName, message, DateTime.UtcNow);
    }
}