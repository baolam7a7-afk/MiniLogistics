namespace MiniLogistics.BLL.DTOs.AdminDashboard;

public class ShopStatisticsDTO
{
    public long ShopId { get; set; }

    public string ShopName { get; set; } = string.Empty;

    public long SellerId { get; set; }

    public string SellerName { get; set; } = string.Empty;

    public int TotalOrders { get; set; }

    public int DeliveredOrders { get; set; }

    public decimal Revenue { get; set; }
}