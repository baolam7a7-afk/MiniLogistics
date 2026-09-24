namespace MiniLogistics.BLL.DTOs.PayoutRequest;

public class CreatePayoutRequestDTO
{
    public long ShopId { get; set; }

    public decimal Amount { get; set; }

    public string BankAccountName { get; set; } = null!;

    public string BankAccountNumber { get; set; } = null!;

    public string BankName { get; set; } = null!;
}