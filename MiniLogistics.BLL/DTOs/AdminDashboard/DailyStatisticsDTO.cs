namespace MiniLogistics.BLL.DTOs.AdminDashboard;

public class DailyStatisticsDTO
{
    public DateTime Date { get; set; }

    public int TotalOrders { get; set; }

    public decimal Revenue { get; set; }
}