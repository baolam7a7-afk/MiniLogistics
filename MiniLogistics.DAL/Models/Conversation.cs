namespace MiniLogistics.DAL.Models;

public class Conversation
{
    public long Id { get; set; }

    public long CustomerUserId { get; set; }

    public long SellerUserId { get; set; }

    public long? ShopId { get; set; }

    public long? ProductId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LastMessageAt { get; set; }

    public User CustomerUser { get; set; } = null!;

    public User SellerUser { get; set; } = null!;

    public Shop? Shop { get; set; }

    public Product? Product { get; set; }

    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
