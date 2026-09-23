namespace MiniLogistics.BLL.DTOs.Payment;

public class PaymentSummaryDTO
{
    public long PaymentId { get; set; }

    public long OrderId { get; set; }

    public string OrderCode { get; set; } = null!;

    public decimal Amount { get; set; }

    public string Method { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime? PaidAt { get; set; }
}