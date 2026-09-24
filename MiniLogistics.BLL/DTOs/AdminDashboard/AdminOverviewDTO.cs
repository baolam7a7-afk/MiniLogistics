namespace MiniLogistics.BLL.DTOs.AdminDashboard;

public class AdminOverviewDTO
{
    public decimal TotalRevenue { get; set; }

    public int TotalOrders { get; set; }

    public int TodayOrders { get; set; }

    public int ThisMonthOrders { get; set; }

    public int TotalUsers { get; set; }

    public int TotalSellers { get; set; }

    public int TotalShops { get; set; }

    public int TotalProducts { get; set; }

    public int TotalRefunds { get; set; }

    public decimal TotalRefundAmount { get; set; }
}