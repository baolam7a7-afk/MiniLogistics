namespace MiniLogistics.BLL.DTOs.SellerDashboard;

public class InventorySummaryDTO
{
    public int TotalVariants { get; set; }

    public int OutOfStockVariants { get; set; }

    public int TotalQuantity { get; set; }

    public int ReservedQuantity { get; set; }

    public int AvailableQuantity { get; set; }
}