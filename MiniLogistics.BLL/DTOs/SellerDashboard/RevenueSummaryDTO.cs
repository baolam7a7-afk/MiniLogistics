namespace MiniLogistics.BLL.DTOs.SellerDashboard;

public class RevenueSummaryDTO
{
    public decimal TotalRevenue { get; set; }

    public decimal TodayRevenue { get; set; }

    public decimal ThisMonthRevenue { get; set; }

    public int TotalOrders { get; set; }

    public int DeliveredOrders { get; set; }

    public int PendingOrders { get; set; }

    public int CancelledOrders { get; set; }
}