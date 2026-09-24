namespace MiniLogistics.BLL.DTOs.SellerDashboard;

public class OrderSummaryDTO
{
    public int Total { get; set; }

    public int Pending { get; set; }

    public int Confirmed { get; set; }

    public int Processing { get; set; }

    public int Shipping { get; set; }

    public int Delivered { get; set; }

    public int Cancelled { get; set; }

    public List<RecentOrderDTO> RecentOrders { get; set; }
        = new();
}