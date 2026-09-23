namespace MiniLogistics.BLL.DTOs.Payment;

public class UpdatePaymentStatusDTO
{
    public string Status { get; set; } = null!;

    public string? ProviderTxnId { get; set; }
}