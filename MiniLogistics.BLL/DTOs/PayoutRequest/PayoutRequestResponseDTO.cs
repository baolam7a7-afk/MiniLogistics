namespace MiniLogistics.BLL.DTOs.PayoutRequest;

public class PayoutRequestResponseDTO
{
    public long Id { get; set; }

    public long ShopId { get; set; }

    public string? ShopName { get; set; }

    public decimal Amount { get; set; }

    public string BankAccountName { get; set; } = null!;

    public string BankAccountNumber { get; set; } = null!;

    public string BankName { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime RequestedAt { get; set; }

    public long? ProcessedByUserId { get; set; }

    public DateTime? ProcessedAt { get; set; }
}