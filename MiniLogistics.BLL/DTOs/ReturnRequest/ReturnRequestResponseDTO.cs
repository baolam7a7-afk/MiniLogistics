namespace MiniLogistics.BLL.DTOs.ReturnRequest;

public class ReturnRequestResponseDTO
{
    public long Id { get; set; }

    public long OrderId { get; set; }

    public string OrderCode { get; set; } = null!;

    public long CustomerId { get; set; }

    public string CustomerEmail { get; set; } = null!;

    public string Reason { get; set; } = null!;

    public string? Description { get; set; }

    public string Status { get; set; } = null!;

    public DateTime RequestedAt { get; set; }

    public long? HandledByUserId { get; set; }

    public string? HandledByUserEmail { get; set; }

    public DateTime? HandledAt { get; set; }
}