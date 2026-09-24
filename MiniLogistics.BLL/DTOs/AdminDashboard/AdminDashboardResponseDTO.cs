namespace MiniLogistics.BLL.DTOs.AdminDashboard;

public class AdminDashboardResponseDTO
{
    public AdminOverviewDTO Overview { get; set; } = new();

    public List<DailyStatisticsDTO> ByDay { get; set; } = new();

    public List<MonthlyStatisticsDTO> ByMonth { get; set; } = new();

    public List<ShopStatisticsDTO> ByShop { get; set; } = new();

    public List<SellerStatisticsDTO> BySeller { get; set; } = new();

    public List<OrderStatusStatisticsDTO> ByOrderStatus { get; set; } = new();
}