namespace MiniLogistics.BLL.DTOs.SellerDashboard;

public class ProductSummaryDTO
{
    public int TotalProducts { get; set; }

    public int ActiveProducts { get; set; }

    public int DraftProducts { get; set; }

    public int OtherProducts { get; set; }

    public int TotalVariants { get; set; }

    public int ActiveVariants { get; set; }
}