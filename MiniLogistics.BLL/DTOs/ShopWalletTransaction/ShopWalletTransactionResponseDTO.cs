namespace MiniLogistics.BLL.DTOs.ShopWalletTransaction;

public class ShopWalletTransactionResponseDTO
{
    public long Id { get; set; }

    public long WalletId { get; set; }

    public long ShopId { get; set; }

    public string ShopName { get; set; } = null!;

    public long? OrderId { get; set; }

    public string Type { get; set; } = null!;

    public decimal Amount { get; set; }

    public decimal? BalanceAfter { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }
}