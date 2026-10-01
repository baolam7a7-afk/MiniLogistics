namespace MiniLogistics.DAL.Models;

public class ChatMessage
{
    public long Id { get; set; }

    public long ConversationId { get; set; }

    public long SenderUserId { get; set; }

    public string Content { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public bool IsRead { get; set; }

    public DateTime? ReadAt { get; set; }

    public Conversation Conversation { get; set; } = null!;

    public User SenderUser { get; set; } = null!;
}
