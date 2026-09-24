namespace MiniLogistics.BLL.DTOs.SellerDashboard;

public class SellerDashboardResponseDTO
{
    public long SellerId { get; set; }

    public int TotalShops { get; set; }

    public List<ShopSummaryDTO> Shops { get; set; } = new();

    public RevenueSummaryDTO Revenue { get; set; } = new();

    public OrderSummaryDTO Orders { get; set; } = new();

    public ProductSummaryDTO Products { get; set; } = new();

    public InventorySummaryDTO Inventory { get; set; } = new();

    public WalletSummaryDTO Wallet { get; set; } = new();

    public RefundSummaryDTO Refunds { get; set; } = new();

    public PayoutSummaryDTO Payouts { get; set; } = new();
}