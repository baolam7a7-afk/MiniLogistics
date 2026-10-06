using Microsoft.EntityFrameworkCore;
using MiniLogistics.BLL.Exceptions;
using MiniLogistics.DAL.Models;
using MiniLogistics.DAL.UnitOfWork;

namespace MiniLogistics.BLL.Services.Chat;

public interface IChatService
{
    Task<ConversationResponseDTO> StartAsync(long customerUserId, long productId);
    Task<IEnumerable<ConversationListItemDTO>> GetMyConversationsAsync(long userId, bool isSeller, bool isAdmin = false);
    Task<IEnumerable<ConversationListItemDTO>> GetPlatformAsync(long userId, bool isAdmin);
    Task<ConversationResponseDTO> OpenPlatformAsync(long userId, long shopId, bool isAdmin);
    Task<ChatMessageResponseDTO> NotifyPlatformAsync(long adminUserId, long shopId, long? productId, string content);
    Task<IEnumerable<ChatMessageResponseDTO>> GetMessagesAsync(long userId, long conversationId, bool isAdmin = false);
    Task<ChatMessageResponseDTO> SendAsync(long userId, long conversationId, string content, bool isAdmin = false);
    Task<long> MarkReadAsync(long userId, long conversationId, bool isAdmin = false);
    Task<bool> IsParticipantAsync(long userId, long conversationId);
}

public class ChatInboxEvent
{
    public long MessageId { get; set; }
    public long ConversationId { get; set; }
    public long SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string? SenderAvatarUrl { get; set; }
    public long ReceiverId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
    public bool FromCustomer { get; set; }
    public string? ShopName { get; set; }
    public string? ProductName { get; set; }
    public string Channel { get; set; } = "shop";
}

public class ConversationResponseDTO
{
    public long Id { get; set; }
    public long CustomerUserId { get; set; }
    public long SellerUserId { get; set; }
    public long? ShopId { get; set; }
    public long? ProductId { get; set; }
    public string? ShopName { get; set; }
    public string? ShopLogoUrl { get; set; }
    public string? ShopDescription { get; set; }
    public string? ShopStatus { get; set; }
    public double? ShopRating { get; set; }
    public string? ProductName { get; set; }
    public string? ProductImageUrl { get; set; }
    public decimal? ProductPrice { get; set; }
    public int? ProductStock { get; set; }
    public string? ProductSummary { get; set; }
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
    public string? SenderAvatarUrl { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public bool IsMine { get; set; }
    public long RecipientUserId { get; set; }
    public long ShopUserId { get; set; }
    public bool FromCustomer { get; set; }
    public string? ShopName { get; set; }
    public string? ProductName { get; set; }
    public string Channel { get; set; } = "shop";
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
            .Include(p => p.ProductImages)
            .Include(p => p.ProductVariants)
                .ThenInclude(v => v.Inventory)
            .FirstOrDefaultAsync(p => p.Id == productId)
            ?? throw new NotFoundException("Sản phẩm không tồn tại.");

        if (product.Shop == null)
            throw new BadRequestException("Sản phẩm chưa gắn shop.");

        var sellerUserId = product.Shop.OwnerUserId;
        if (sellerUserId == customerUserId)
            throw new BadRequestException("Bạn không thể chat với chính mình.");

        await MergeShopThreadAsync(customerUserId, product.ShopId);

        var existing = await DetailQuery()
            .FirstOrDefaultAsync(c =>
                c.CustomerUserId == customerUserId &&
                c.ShopId == product.ShopId);

        if (existing != null)
        {
            if (existing.ProductId != productId)
            {
                existing.ProductId = productId;
                existing.Product = product;
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
            Channel = "shop",
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
        bool isSeller,
        bool isAdmin = false)
    {
        var query = DetailQuery()
            .AsNoTracking()
            .Include(c => c.Messages)
            .AsQueryable();

        query = query.Where(c => c.Channel != "platform");

        if (!isAdmin)
        {
            query = isSeller
                ? query.Where(c => c.SellerUserId == userId)
                : query.Where(c => c.CustomerUserId == userId);
        }

        await MergeDuplicateThreadsAsync(query);

        var asShop = isAdmin || isSeller;

        var list = await query
            .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
            .ToListAsync();
        var ratings = await ShopRatingsAsync(list.Select(c => c.ShopId));

        return list.Select(c =>
        {
            var last = c.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault();
            var unread = asShop
                ? c.Messages.Count(m => !m.IsRead && m.SenderUserId == c.CustomerUserId)
                : c.Messages.Count(m => !m.IsRead && m.SenderUserId != userId);
            var peer = asShop ? c.CustomerUser : c.SellerUser;
            var item = new ConversationListItemDTO
            {
                LastMessagePreview = last?.Content
            };
            Fill(item, c, peer?.FullName ?? peer?.Email, unread, ratings);
            return item;
        });
    }

    public async Task<IEnumerable<ChatMessageResponseDTO>> GetMessagesAsync(
        long userId,
        long conversationId,
        bool isAdmin = false)
    {
        var conversation = await EnsureParticipant(userId, conversationId, isAdmin);

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
        string content,
        bool isAdmin = false)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new BadRequestException("Tin nhắn không được để trống.");

        if (content.Length > 4000)
            throw new BadRequestException("Tin nhắn quá dài.");

        var conversation = await EnsureParticipant(userId, conversationId, isAdmin);

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
        var dto = MapMessage(message, userId);
        dto.RecipientUserId = conversation.CustomerUserId == userId
            ? conversation.SellerUserId
            : conversation.CustomerUserId;
        dto.ShopUserId = conversation.SellerUserId;
        dto.FromCustomer = userId == conversation.CustomerUserId;
        dto.ShopName = conversation.Shop?.Name;
        dto.ProductName = conversation.Product?.Name;
        dto.Channel = conversation.Channel;
        return dto;
    }

    public async Task<IEnumerable<ConversationListItemDTO>> GetPlatformAsync(long userId, bool isAdmin)
    {
        var threads = await DetailQuery()
            .AsNoTracking()
            .Include(c => c.Messages)
            .Where(c => c.Channel == "platform")
            .Where(c => isAdmin || c.CustomerUserId == userId)
            .ToListAsync();

        if (!isAdmin)
        {
            return threads
                .Select(thread => MapPlatform(thread, userId, false))
                .OrderByDescending(item => item.LastMessageAt ?? item.CreatedAt);
        }

        var shops = await _uow.Shops.Query()
            .AsNoTracking()
            .Include(shop => shop.OwnerUser)
            .OrderBy(shop => shop.Name)
            .ToListAsync();
        var byShop = threads
            .Where(thread => thread.ShopId.HasValue)
            .GroupBy(thread => thread.ShopId!.Value)
            .ToDictionary(group => group.Key, group => group.First());

        var items = shops.Select(shop =>
        {
            if (byShop.TryGetValue(shop.Id, out var thread))
            {
                return MapPlatform(thread, userId, true);
            }

            return new ConversationListItemDTO
            {
                ShopId = shop.Id,
                ShopName = shop.Name,
                ShopLogoUrl = shop.LogoUrl,
                ShopStatus = shop.Status,
                PeerName = shop.OwnerUser?.FullName ?? shop.OwnerUser?.Email ?? "Seller"
            };
        });

        return items
            .OrderByDescending(item => item.LastMessageAt ?? DateTime.MinValue)
            .ThenBy(item => item.ShopName);
    }

    public async Task<ConversationResponseDTO> OpenPlatformAsync(long userId, long shopId, bool isAdmin)
    {
        var shop = await _uow.Shops.Query()
            .Include(item => item.OwnerUser)
            .FirstOrDefaultAsync(item => item.Id == shopId)
            ?? throw new NotFoundException("Shop không tồn tại.");

        if (!isAdmin && shop.OwnerUserId != userId)
        {
            throw new ForbiddenException("Bạn không có quyền chat cho cửa hàng này.");
        }

        var conversation = await DetailQuery()
            .FirstOrDefaultAsync(item => item.Channel == "platform" && item.ShopId == shopId);

        if (conversation == null)
        {
            var adminId = isAdmin ? userId : await FirstAdminIdAsync();
            conversation = new Conversation
            {
                CustomerUserId = shop.OwnerUserId,
                SellerUserId = adminId,
                ShopId = shopId,
                Channel = "platform",
                CreatedAt = DateTime.UtcNow
            };
            await _uow.Conversations.AddAsync(conversation);
            await _uow.SaveChangesAsync();
            conversation = await DetailQuery().FirstAsync(item => item.Id == conversation.Id);
        }

        return await MapConversation(conversation, userId);
    }

    private async Task<long> FirstAdminIdAsync()
    {
        var adminId = await (
            from link in _uow.UserRoles.Query().AsNoTracking()
            join role in _uow.Roles.Query().AsNoTracking() on link.RoleId equals role.Id
            where role.Name == "admin"
            select link.UserId
        ).FirstOrDefaultAsync();

        if (adminId <= 0)
        {
            throw new BadRequestException("Chưa có tài khoản admin để nhận tin nhắn.");
        }

        return adminId;
    }

    private static ConversationListItemDTO MapPlatform(Conversation thread, long userId, bool isAdmin)
    {
        var last = thread.Messages.OrderByDescending(message => message.CreatedAt).FirstOrDefault();
        var unread = thread.Messages.Count(message => !message.IsRead && message.SenderUserId != userId);
        var peer = isAdmin
            ? thread.CustomerUser?.FullName ?? thread.CustomerUser?.Email ?? "Seller"
            : "Admin";
        var item = new ConversationListItemDTO
        {
            LastMessagePreview = last?.Content
        };
        Fill(item, thread, peer, unread, new());
        return item;
    }

    public async Task<ChatMessageResponseDTO> NotifyPlatformAsync(
        long adminUserId,
        long shopId,
        long? productId,
        string content)
    {
        var shop = await _uow.Shops.Query()
            .Include(item => item.OwnerUser)
            .FirstOrDefaultAsync(item => item.Id == shopId)
            ?? throw new NotFoundException("Shop không tồn tại.");

        if (shop.OwnerUserId == adminUserId)
        {
            throw new BadRequestException("Không gửi thông báo cho chính admin.");
        }

        var conversation = await _uow.Conversations.Query()
            .Include(item => item.Shop)
            .Include(item => item.Product)
            .FirstOrDefaultAsync(item => item.Channel == "platform" && item.ShopId == shopId);

        if (conversation == null)
        {
            conversation = new Conversation
            {
                CustomerUserId = shop.OwnerUserId,
                SellerUserId = adminUserId,
                ShopId = shopId,
                ProductId = productId,
                Channel = "platform",
                CreatedAt = DateTime.UtcNow
            };
            await _uow.Conversations.AddAsync(conversation);
            await _uow.SaveChangesAsync();
            conversation.Shop = shop;
        }
        else if (productId.HasValue && conversation.ProductId != productId)
        {
            conversation.ProductId = productId;
            conversation.Product = null;
        }

        if (productId.HasValue && conversation.Product == null)
        {
            conversation.Product = await _uow.Products.GetByIdAsync(productId.Value);
        }

        var message = new ChatMessage
        {
            ConversationId = conversation.Id,
            SenderUserId = adminUserId,
            Content = content.Trim(),
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };
        await _uow.ChatMessages.AddAsync(message);
        conversation.LastMessageAt = message.CreatedAt;
        await _uow.SaveChangesAsync();

        var sender = await _uow.Users.GetByIdAsync(adminUserId);
        message.SenderUser = sender!;
        var dto = MapMessage(message, adminUserId);
        dto.RecipientUserId = shop.OwnerUserId;
        dto.ShopUserId = shop.OwnerUserId;
        dto.FromCustomer = false;
        dto.ShopName = shop.Name;
        dto.ProductName = conversation.Product?.Name;
        dto.Channel = "platform";
        return dto;
    }

    public async Task<long> MarkReadAsync(long userId, long conversationId, bool isAdmin = false)
    {
        var conversation = await EnsureParticipant(userId, conversationId, isAdmin);
        var asShop = isAdmin || conversation.SellerUserId == userId;

        var unread = await _uow.ChatMessages.Query()
            .Where(m =>
                m.ConversationId == conversationId &&
                !m.IsRead &&
                (asShop
                    ? m.SenderUserId == conversation.CustomerUserId
                    : m.SenderUserId != userId))
            .ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var m in unread)
        {
            m.IsRead = true;
            m.ReadAt = now;
        }

        if (unread.Count > 0)
            await _uow.SaveChangesAsync();

        return conversation.SellerUserId;
    }

    public Task<bool> IsParticipantAsync(long userId, long conversationId) =>
        _uow.Conversations.Query().AnyAsync(conversation =>
            conversation.Id == conversationId &&
            (conversation.CustomerUserId == userId || conversation.SellerUserId == userId));

    private async Task<Conversation> EnsureParticipant(long userId, long conversationId, bool isAdmin = false)
    {
        var conversation = await _uow.Conversations.Query()
            .Include(c => c.Shop)
            .Include(c => c.Product)
            .FirstOrDefaultAsync(c => c.Id == conversationId)
            ?? throw new NotFoundException("Cuộc trò chuyện không tồn tại.");

        if (isAdmin || conversation.CustomerUserId == userId || conversation.SellerUserId == userId)
            return conversation;

        throw new ForbiddenException("Bạn không có quyền truy cập cuộc trò chuyện này.");
    }

    private async Task<ConversationResponseDTO> MapConversation(Conversation c, long viewerId)
    {
        var unread = await _uow.ChatMessages.CountAsync(m =>
            m.ConversationId == c.Id &&
            m.SenderUserId != viewerId &&
            !m.IsRead);

        var dto = new ConversationResponseDTO();
        var ratings = await ShopRatingsAsync(new long?[] { c.ShopId });
        Fill(dto, c, null, unread, ratings);
        return dto;
    }

    private async Task MergeDuplicateThreadsAsync(IQueryable<Conversation> scope)
    {
        var rows = await scope
            .Where(c => c.ShopId != null && c.Channel != "platform")
            .Select(c => new { c.CustomerUserId, c.ShopId })
            .ToListAsync();

        var duplicates = rows
            .GroupBy(row => (row.CustomerUserId, ShopId: row.ShopId!.Value))
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        foreach (var key in duplicates)
        {
            await MergeShopThreadAsync(key.CustomerUserId, key.ShopId);
        }
    }

    private async Task MergeShopThreadAsync(long customerUserId, long shopId)
    {
        var threads = await _uow.Conversations.Query()
            .Where(c => c.CustomerUserId == customerUserId && c.ShopId == shopId && c.Channel != "platform")
            .OrderBy(c => c.CreatedAt)
            .ThenBy(c => c.Id)
            .ToListAsync();

        if (threads.Count <= 1)
        {
            return;
        }

        var primary = threads[0];
        foreach (var extra in threads.Skip(1))
        {
            var messages = await _uow.ChatMessages.Query()
                .Where(m => m.ConversationId == extra.Id)
                .ToListAsync();
            foreach (var message in messages)
            {
                message.ConversationId = primary.Id;
            }

            if (extra.LastMessageAt > (primary.LastMessageAt ?? DateTime.MinValue))
            {
                primary.LastMessageAt = extra.LastMessageAt;
            }
        }

        await _uow.SaveChangesAsync();

        foreach (var extra in threads.Skip(1))
        {
            _uow.Conversations.Delete(extra);
        }

        await _uow.SaveChangesAsync();
    }

    private IQueryable<Conversation> DetailQuery() =>
        _uow.Conversations.Query()
            .Include(c => c.Shop)
            .Include(c => c.CustomerUser)
            .Include(c => c.SellerUser)
            .Include(c => c.Product)
                .ThenInclude(p => p!.ProductImages)
            .Include(c => c.Product)
                .ThenInclude(p => p!.ProductVariants)
                    .ThenInclude(v => v.Inventory);

    private async Task<Dictionary<long, double>> ShopRatingsAsync(IEnumerable<long?> shopIds)
    {
        var ids = shopIds.Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        if (ids.Count == 0)
        {
            return new();
        }

        var rows = await (
            from review in _uow.Reviews.Query().AsNoTracking()
            join product in _uow.Products.Query().AsNoTracking() on review.ProductId equals product.Id
            where ids.Contains(product.ShopId)
            group review by product.ShopId into grouped
            select new { ShopId = grouped.Key, Avg = grouped.Average(item => (double)item.Rating) }
        ).ToListAsync();

        return rows.ToDictionary(item => item.ShopId, item => Math.Round(item.Avg, 1));
    }

    private static void Fill(
        ConversationResponseDTO dto,
        Conversation conversation,
        string? peerName,
        int unread,
        Dictionary<long, double> ratings)
    {
        var product = conversation.Product;
        var image = product?.ProductImages.OrderBy(item => item.SortOrder).FirstOrDefault();
        var variants = product?.ProductVariants.Where(item => item.IsActive).ToList() ?? new();
        var price = variants.Count == 0 ? (decimal?)null : variants.Min(item => item.Price);
        var stock = variants.Sum(item =>
            Math.Max(0, (item.Inventory?.Quantity ?? 0) - (item.Inventory?.ReservedQuantity ?? 0)));

        dto.Id = conversation.Id;
        dto.CustomerUserId = conversation.CustomerUserId;
        dto.SellerUserId = conversation.SellerUserId;
        dto.ShopId = conversation.ShopId;
        dto.ProductId = conversation.ProductId;
        dto.ShopName = conversation.Shop?.Name;
        dto.ShopLogoUrl = conversation.Shop?.LogoUrl;
        dto.ShopDescription = conversation.Shop?.Description;
        dto.ShopStatus = conversation.Shop?.Status;
        dto.ShopRating = conversation.ShopId is long shopId && ratings.TryGetValue(shopId, out var rating)
            ? rating
            : null;
        dto.ProductName = product?.Name;
        dto.ProductImageUrl = image?.Url;
        dto.ProductPrice = price;
        dto.ProductStock = product == null ? null : stock;
        dto.ProductSummary = string.IsNullOrWhiteSpace(product?.Description)
            ? null
            : (product!.Description!.Length <= 140 ? product.Description : product.Description[..140]);
        dto.PeerName = peerName;
        dto.CreatedAt = conversation.CreatedAt;
        dto.LastMessageAt = conversation.LastMessageAt;
        dto.UnreadCount = unread;
    }

    private static ChatMessageResponseDTO MapMessage(ChatMessage m, long viewerId) => new()
    {
        Id = m.Id,
        ConversationId = m.ConversationId,
        SenderUserId = m.SenderUserId,
        SenderName = m.SenderUser?.FullName ?? m.SenderUser?.Email ?? "User",
        SenderAvatarUrl = m.SenderUser?.AvatarUrl,
        Content = m.Content,
        CreatedAt = m.CreatedAt,
        IsRead = m.IsRead,
        ReadAt = m.ReadAt,
        IsMine = m.SenderUserId == viewerId
    };
}
