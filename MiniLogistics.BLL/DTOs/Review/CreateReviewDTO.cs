namespace MiniLogistics.BLL.DTOs.Review;

public class CreateReviewDTO
{
    public long OrderId { get; set; }

    public long OrderItemId { get; set; }

    public long ProductId { get; set; }

    public int Rating { get; set; }

    public string? Content { get; set; }
}