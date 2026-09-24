namespace MiniLogistics.BLL.DTOs.SellerDashboard;

public class PayoutSummaryDTO
{
    public int TotalRequests { get; set; }

    public int RequestedRequests { get; set; }

    public int ProcessedRequests { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal RequestedAmount { get; set; }

    public decimal ProcessedAmount { get; set; }
}