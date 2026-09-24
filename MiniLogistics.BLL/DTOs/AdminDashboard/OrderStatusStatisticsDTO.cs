namespace MiniLogistics.BLL.DTOs.AdminDashboard;

public class OrderStatusStatisticsDTO
{
    public string Status { get; set; } = string.Empty;

    public int TotalOrders { get; set; }

    public decimal TotalAmount { get; set; }
}