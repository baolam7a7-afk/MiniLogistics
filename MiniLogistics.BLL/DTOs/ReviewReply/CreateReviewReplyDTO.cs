namespace MiniLogistics.BLL.DTOs.ReviewReply;

public class CreateReviewReplyDTO
{
    public long ReviewId { get; set; }

    public long ShopId { get; set; }

    public string Content { get; set; } = null!;
}