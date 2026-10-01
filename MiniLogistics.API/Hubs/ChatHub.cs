using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MiniLogistics.API.Hubs;

[Authorize]
public class ChatHub : Hub
{
    public static string GroupName(long conversationId) =>
        $"conversation-{conversationId}";

    public async Task JoinConversation(long conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(conversationId));
    }

    public async Task LeaveConversation(long conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(conversationId));
    }

    public override Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            return Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
        }

        return base.OnConnectedAsync();
    }
}
