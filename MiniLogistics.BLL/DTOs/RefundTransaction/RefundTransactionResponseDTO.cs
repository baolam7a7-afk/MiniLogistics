namespace MiniLogistics.BLL.DTOs.RefundTransaction;

public class RefundTransactionResponseDTO
{
    public long Id { get; set; }

    public long ReturnRequestId { get; set; }

    public long OrderId { get; set; }

    public string OrderCode { get; set; } = null!;

    public long CustomerId { get; set; }

    public string? CustomerEmail { get; set; }

    public decimal Amount { get; set; }

    public string Method { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }
}