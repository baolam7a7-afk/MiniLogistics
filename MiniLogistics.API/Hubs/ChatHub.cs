using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MiniLogistics.BLL.Services.Chat;

namespace MiniLogistics.API.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IChatService _chat;

    public ChatHub(IChatService chat)
    {
        _chat = chat;
    }

    public static string GroupName(long conversationId) =>
        $"conversation-{conversationId}";

    public static string UserGroup(long userId) =>
        $"user-{userId}";

    public static string AdminGroup => "admins";

    public static string ShipperGroup => "shippers";

    public async Task JoinConversation(long conversationId)
    {
        var userId = GetUserId();
        var isAdmin = Context.User?.IsInRole("admin") == true;
        if (userId == null || (!isAdmin && !await _chat.IsParticipantAsync(userId.Value, conversationId)))
        {
            throw new HubException("Bạn không có quyền vào cuộc trò chuyện này.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(conversationId));
    }

    public async Task LeaveConversation(long conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(conversationId));
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
        }

        if (Context.User?.IsInRole("admin") == true)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, AdminGroup);
        }

        if (Context.User?.IsInRole("shipper") == true)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, ShipperGroup);
        }

        await base.OnConnectedAsync();
    }

    private long? GetUserId()
    {
        var value = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return long.TryParse(value, out var userId) ? userId : null;
    }
}
