namespace MiniLogistics.BLL.DTOs.Order;

public class OrderStatusLogResponseDTO
{
    public long Id { get; set; }

    public string? FromStatus { get; set; }

    public string ToStatus { get; set; } = string.Empty;

    public string? Message { get; set; }

    public long? CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }
}