namespace MiniLogistics.BLL.DTOs.AdminDashboard;

public class MonthlyStatisticsDTO
{
    public int Year { get; set; }

    public int Month { get; set; }

    public int TotalOrders { get; set; }

    public decimal Revenue { get; set; }
}