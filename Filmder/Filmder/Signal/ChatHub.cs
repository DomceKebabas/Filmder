using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Filmder.Signal;

[Authorize]
public class ChatHub : Hub
{

    public async Task JoinGroup(int groupId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupId.ToString());
    }

    public async Task SendMessageToGroup(int groupId, string message)
    {
        var userName = Context.User.FindFirstValue(ClaimTypes.Name) ?? "Unknown";
        await Clients.GroupExcept(groupId.ToString(), Context.ConnectionId)
            .SendAsync("getMessage", userName, message);
    }
    public async Task gameCreated(string groupId, string creatorName)
    {
        await Clients.Group(groupId)
            .SendAsync("gameCreated", $"{creatorName} created a new movie game!");
    }
}