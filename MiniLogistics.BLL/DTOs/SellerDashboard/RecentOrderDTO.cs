namespace MiniLogistics.BLL.DTOs.SellerDashboard;

public class RecentOrderDTO
{
    public long OrderId { get; set; }

    public string OrderCode { get; set; } = string.Empty;

    public long ShopId { get; set; }

    public string ShopName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public decimal Total { get; set; }

    public DateTime PlacedAt { get; set; }
}