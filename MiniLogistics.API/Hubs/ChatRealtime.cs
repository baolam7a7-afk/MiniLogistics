using Microsoft.AspNetCore.SignalR;
using MiniLogistics.BLL.Services.Chat;

namespace MiniLogistics.API.Hubs;

public class ChatRealtime
{
    private readonly IHubContext<ChatHub> _hub;

    public ChatRealtime(IHubContext<ChatHub> hub)
    {
        _hub = hub;
    }

    public async Task PublishAsync(ChatMessageResponseDTO message)
    {
        if (message.RecipientUserId <= 0)
        {
            return;
        }

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
}
