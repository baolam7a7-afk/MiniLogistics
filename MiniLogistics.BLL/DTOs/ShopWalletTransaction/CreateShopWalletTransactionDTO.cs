namespace MiniLogistics.BLL.DTOs.ShopWalletTransaction;

public class CreateShopWalletTransactionDTO
{
    public long WalletId { get; set; }

    public long? OrderId { get; set; }

    public string Type { get; set; } = null!;

    public decimal Amount { get; set; }

    public string? Description { get; set; }
}