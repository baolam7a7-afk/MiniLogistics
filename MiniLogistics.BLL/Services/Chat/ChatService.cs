using Microsoft.EntityFrameworkCore;
using MiniLogistics.BLL.Exceptions;
using MiniLogistics.DAL.Models;
using MiniLogistics.DAL.UnitOfWork;

namespace MiniLogistics.BLL.Services.Chat;

public interface IChatService
{
    Task<ConversationResponseDTO> StartAsync(long customerUserId, long productId);
    Task<IEnumerable<ConversationListItemDTO>> GetMyConversationsAsync(long userId, bool isSeller);
    Task<IEnumerable<ChatMessageResponseDTO>> GetMessagesAsync(long userId, long conversationId);
    Task<ChatMessageResponseDTO> SendAsync(long userId, long conversationId, string content);
    Task MarkReadAsync(long userId, long conversationId);
}

public class ConversationResponseDTO
{
    public long Id { get; set; }
    public long CustomerUserId { get; set; }
    public long SellerUserId { get; set; }
    public long? ShopId { get; set; }
    public long? ProductId { get; set; }
    public string? ShopName { get; set; }
    public string? ProductName { get; set; }
    public string? PeerName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public int UnreadCount { get; set; }
}

public class ConversationListItemDTO : ConversationResponseDTO
{
    public string? LastMessagePreview { get; set; }
}

public class ChatMessageResponseDTO
{
    public long Id { get; set; }
    public long ConversationId { get; set; }
    public long SenderUserId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public bool IsMine { get; set; }
}

public class ChatService : IChatService
{
    private readonly IUnitOfWork _uow;

    public ChatService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ConversationResponseDTO> StartAsync(long customerUserId, long productId)
    {
        var product = await _uow.Products.Query()
            .Include(p => p.Shop)
            .FirstOrDefaultAsync(p => p.Id == productId)
            ?? throw new NotFoundException("Sản phẩm không tồn tại.");

        if (product.Shop == null)
            throw new BadRequestException("Sản phẩm chưa gắn shop.");

        var sellerUserId = product.Shop.OwnerUserId;
        if (sellerUserId == customerUserId)
            throw new BadRequestException("Bạn không thể chat với chính mình.");

        var existing = await _uow.Conversations.Query()
            .Include(c => c.Shop)
            .Include(c => c.Product)
            .FirstOrDefaultAsync(c =>
                c.CustomerUserId == customerUserId &&
                c.SellerUserId == sellerUserId &&
                c.ShopId == product.ShopId);

        if (existing != null)
        {
            if (existing.ProductId != productId)
            {
                existing.ProductId = productId;
                await _uow.SaveChangesAsync();
            }

            return await MapConversation(existing, customerUserId);
        }

        var conversation = new Conversation
        {
            CustomerUserId = customerUserId,
            SellerUserId = sellerUserId,
            ShopId = product.ShopId,
            ProductId = productId,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.Conversations.AddAsync(conversation);
        await _uow.SaveChangesAsync();

        conversation.Shop = product.Shop;
        conversation.Product = product;
        return await MapConversation(conversation, customerUserId);
    }

    public async Task<IEnumerable<ConversationListItemDTO>> GetMyConversationsAsync(
        long userId,
        bool isSeller)
    {
        var query = _uow.Conversations.Query()
            .AsNoTracking()
            .Include(c => c.Shop)
            .Include(c => c.Product)
            .Include(c => c.CustomerUser)
            .Include(c => c.SellerUser)
            .Include(c => c.Messages)
            .AsQueryable();

        query = isSeller
            ? query.Where(c => c.SellerUserId == userId)
            : query.Where(c => c.CustomerUserId == userId);

        var list = await query
            .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
            .ToListAsync();

        return list.Select(c =>
        {
            var last = c.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault();
            var unread = c.Messages.Count(m => !m.IsRead && m.SenderUserId != userId);
            var peer = isSeller ? c.CustomerUser : c.SellerUser;

            return new ConversationListItemDTO
            {
                Id = c.Id,
                CustomerUserId = c.CustomerUserId,
                SellerUserId = c.SellerUserId,
                ShopId = c.ShopId,
                ProductId = c.ProductId,
                ShopName = c.Shop?.Name,
                ProductName = c.Product?.Name,
                PeerName = peer?.FullName ?? peer?.Email,
                CreatedAt = c.CreatedAt,
                LastMessageAt = c.LastMessageAt,
                UnreadCount = unread,
                LastMessagePreview = last?.Content
            };
        });
    }

    public async Task<IEnumerable<ChatMessageResponseDTO>> GetMessagesAsync(
        long userId,
        long conversationId)
    {
        var conversation = await EnsureParticipant(userId, conversationId);

        var messages = await _uow.ChatMessages.Query()
            .AsNoTracking()
            .Include(m => m.SenderUser)
            .Where(m => m.ConversationId == conversation.Id)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

        return messages.Select(m => MapMessage(m, userId));
    }

    public async Task<ChatMessageResponseDTO> SendAsync(
        long userId,
        long conversationId,
        string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new BadRequestException("Tin nhắn không được để trống.");

        if (content.Length > 4000)
            throw new BadRequestException("Tin nhắn quá dài.");

        var conversation = await EnsureParticipant(userId, conversationId);

        var message = new ChatMessage
        {
            ConversationId = conversation.Id,
            SenderUserId = userId,
            Content = content.Trim(),
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };

        await _uow.ChatMessages.AddAsync(message);
        conversation.LastMessageAt = message.CreatedAt;
        await _uow.SaveChangesAsync();

        var sender = await _uow.Users.GetByIdAsync(userId);
        message.SenderUser = sender!;
        return MapMessage(message, userId);
    }

    public async Task MarkReadAsync(long userId, long conversationId)
    {
        await EnsureParticipant(userId, conversationId);

        var unread = await _uow.ChatMessages.Query()
            .Where(m =>
                m.ConversationId == conversationId &&
                m.SenderUserId != userId &&
                !m.IsRead)
            .ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var m in unread)
        {
            m.IsRead = true;
            m.ReadAt = now;
        }

        if (unread.Count > 0)
            await _uow.SaveChangesAsync();
    }

    private async Task<Conversation> EnsureParticipant(long userId, long conversationId)
    {
        var conversation = await _uow.Conversations.Query()
            .Include(c => c.Shop)
            .Include(c => c.Product)
            .FirstOrDefaultAsync(c => c.Id == conversationId)
            ?? throw new NotFoundException("Cuộc trò chuyện không tồn tại.");

        if (conversation.CustomerUserId != userId && conversation.SellerUserId != userId)
            throw new ForbiddenException("Bạn không có quyền truy cập cuộc trò chuyện này.");

        return conversation;
    }

    private async Task<ConversationResponseDTO> MapConversation(Conversation c, long viewerId)
    {
        var unread = await _uow.ChatMessages.CountAsync(m =>
            m.ConversationId == c.Id &&
            m.SenderUserId != viewerId &&
            !m.IsRead);

        return new ConversationResponseDTO
        {
            Id = c.Id,
            CustomerUserId = c.CustomerUserId,
            SellerUserId = c.SellerUserId,
            ShopId = c.ShopId,
            ProductId = c.ProductId,
            ShopName = c.Shop?.Name,
            ProductName = c.Product?.Name,
            CreatedAt = c.CreatedAt,
            LastMessageAt = c.LastMessageAt,
            UnreadCount = unread
        };
    }

    private static ChatMessageResponseDTO MapMessage(ChatMessage m, long viewerId) => new()
    {
        Id = m.Id,
        ConversationId = m.ConversationId,
        SenderUserId = m.SenderUserId,
        SenderName = m.SenderUser?.FullName ?? m.SenderUser?.Email ?? "User",
        Content = m.Content,
        CreatedAt = m.CreatedAt,
        IsRead = m.IsRead,
        ReadAt = m.ReadAt,
        IsMine = m.SenderUserId == viewerId
    };
}
