namespace MiniLogistics.BLL.DTOs.Payment;

public class PaymentResponseDTO
{
    public long Id { get; set; }

    public long OrderId { get; set; }

    public string OrderCode { get; set; } = null!;

    public long CustomerId { get; set; }

    public long ShopId { get; set; }

    public string? Provider { get; set; }

    public string Method { get; set; } = null!;

    public decimal Amount { get; set; }

    public string Status { get; set; } = null!;

    public string? ProviderTxnId { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime CreatedAt { get; set; }
}