using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MiniLogistics.API.Hubs;
using MiniLogistics.BLL.Services.Chat;

namespace MiniLogistics.API.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly IHubContext<ChatHub> _hub;

    public ChatController(IChatService chatService, IHubContext<ChatHub> hub)
    {
        _chatService = chatService;
        _hub = hub;
    }

    private long GetUserId() =>
        long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool IsSeller() =>
        User.IsInRole("seller") || User.IsInRole("admin");

    [HttpPost("start")]
    [Authorize(Roles = "customer")]
    public async Task<IActionResult> Start([FromBody] StartChatRequest request)
    {
        var result = await _chatService.StartAsync(GetUserId(), request.ProductId);
        return Ok(result);
    }

    [HttpGet("platform")]
    [Authorize(Roles = "admin,seller")]
    public async Task<IActionResult> Platform() =>
        Ok(await _chatService.GetPlatformAsync(GetUserId(), User.IsInRole("admin")));

    [HttpPost("platform/{shopId:long}/open")]
    [Authorize(Roles = "admin,seller")]
    public async Task<IActionResult> OpenPlatform(long shopId) =>
        Ok(await _chatService.OpenPlatformAsync(GetUserId(), shopId, User.IsInRole("admin")));

    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations()
    {
        var isAdmin = User.IsInRole("admin");
        var asSeller = User.IsInRole("seller");
        return Ok(await _chatService.GetMyConversationsAsync(GetUserId(), asSeller, isAdmin));
    }

    [HttpGet("conversations/{id:long}/messages")]
    public async Task<IActionResult> GetMessages(long id)
    {
        var userId = GetUserId();
        var isAdmin = User.IsInRole("admin");
        var messages = await _chatService.GetMessagesAsync(userId, id, isAdmin);
        var sellerId = await _chatService.MarkReadAsync(userId, id, isAdmin);
        await PublishReadAsync(id, userId, sellerId);
        return Ok(messages);
    }

    [HttpPost("conversations/{id:long}/messages")]
    public async Task<IActionResult> Send(long id, [FromBody] SendChatMessageRequest request)
    {
        var userId = GetUserId();
        var message = await _chatService.SendAsync(userId, id, request.Content, User.IsInRole("admin"));

        if (message.RecipientUserId > 0)
        {
            var notice = new ChatInboxEvent
            {
                MessageId = message.Id,
                ConversationId = message.ConversationId,
                SenderId = message.SenderUserId,
                SenderName = message.SenderName,
                SenderAvatarUrl = message.SenderAvatarUrl,
                ReceiverId = message.RecipientUserId,
                Content = message.Content,
                CreatedAt = message.CreatedAt,
                IsRead = message.IsRead,
                FromCustomer = message.FromCustomer,
                ShopName = message.ShopName,
                ProductName = message.ProductName,
                Channel = message.Channel
            };

            await _hub.Clients.Group(ChatHub.GroupName(message.ConversationId))
                .SendAsync("InboxChanged", notice);
            await _hub.Clients.Group(ChatHub.UserGroup(message.RecipientUserId))
                .SendAsync("InboxChanged", notice);
            await _hub.Clients.Group(ChatHub.AdminGroup)
                .SendAsync("InboxChanged", notice);
        }

        return Ok(message);
    }

    [HttpPost("conversations/{id:long}/read")]
    public async Task<IActionResult> MarkRead(long id)
    {
        var userId = GetUserId();
        var sellerId = await _chatService.MarkReadAsync(userId, id, User.IsInRole("admin"));
        await PublishReadAsync(id, userId, sellerId);
        return Ok(new { message = "Đã đánh dấu đã đọc." });
    }

    private async Task PublishReadAsync(long conversationId, long userId, long sellerId)
    {
        var notice = new ChatInboxEvent
        {
            ConversationId = conversationId,
            ReceiverId = userId,
            IsRead = true
        };
        await _hub.Clients.Group(ChatHub.UserGroup(userId)).SendAsync("InboxChanged", notice);
        if (sellerId > 0 && sellerId != userId)
        {
            await _hub.Clients.Group(ChatHub.UserGroup(sellerId)).SendAsync("InboxChanged", notice);
        }

        await _hub.Clients.Group(ChatHub.AdminGroup).SendAsync("InboxChanged", notice);
    }
}

public class StartChatRequest
{
    public long ProductId { get; set; }
}

public class SendChatMessageRequest
{
    public string Content { get; set; } = string.Empty;
}
