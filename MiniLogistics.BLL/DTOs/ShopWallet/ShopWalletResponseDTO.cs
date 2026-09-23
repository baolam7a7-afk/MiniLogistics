namespace MiniLogistics.BLL.DTOs.ShopWallet;

public class ShopWalletResponseDTO
{
    public long Id { get; set; }

    public long ShopId { get; set; }

    public string ShopName { get; set; } = null!;

    public long OwnerUserId { get; set; }

    public decimal Balance { get; set; }

    public DateTime? UpdatedAt { get; set; }
}