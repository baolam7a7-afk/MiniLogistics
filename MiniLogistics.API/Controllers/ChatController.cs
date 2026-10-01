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

    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations()
    {
        var asSeller = User.IsInRole("seller");
        return Ok(await _chatService.GetMyConversationsAsync(GetUserId(), asSeller));
    }

    [HttpGet("conversations/{id:long}/messages")]
    public async Task<IActionResult> GetMessages(long id)
    {
        var userId = GetUserId();
        var messages = await _chatService.GetMessagesAsync(userId, id);
        await _chatService.MarkReadAsync(userId, id);
        return Ok(messages);
    }

    [HttpPost("conversations/{id:long}/messages")]
    public async Task<IActionResult> Send(long id, [FromBody] SendChatMessageRequest request)
    {
        var userId = GetUserId();
        var message = await _chatService.SendAsync(userId, id, request.Content);

        await _hub.Clients.Group(ChatHub.GroupName(id))
            .SendAsync("ReceiveMessage", message);

        return Ok(message);
    }

    [HttpPost("conversations/{id:long}/read")]
    public async Task<IActionResult> MarkRead(long id)
    {
        await _chatService.MarkReadAsync(GetUserId(), id);
        return Ok(new { message = "Đã đánh dấu đã đọc." });
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
