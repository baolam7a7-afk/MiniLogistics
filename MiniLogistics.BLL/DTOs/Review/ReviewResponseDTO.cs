namespace MiniLogistics.BLL.DTOs.Review;

public class ReviewResponseDTO
{
    public long Id { get; set; }

    public long OrderId { get; set; }

    public long OrderItemId { get; set; }

    public long ProductId { get; set; }

    public long CustomerId { get; set; }

    public int Rating { get; set; }

    public string? Content { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}