namespace MiniLogistics.BLL.DTOs.ReviewReply;

public class ReviewReplyResponseDTO
{
    public long Id { get; set; }

    public long ReviewId { get; set; }

    public long ShopId { get; set; }

    public long RepliedByUserId { get; set; }

    public string Content { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}