namespace MiniLogistics.BLL.DTOs.AdminDashboard;

public class SellerStatisticsDTO
{
    public long SellerId { get; set; }

    public string SellerName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public int TotalShops { get; set; }

    public int TotalOrders { get; set; }

    public int DeliveredOrders { get; set; }

    public decimal Revenue { get; set; }
}