namespace MiniLogistics.BLL.DTOs.SellerDashboard;

public class RefundSummaryDTO
{
    public int TotalRefunds { get; set; }

    public int PendingRefunds { get; set; }

    public int CompletedRefunds { get; set; }

    public int FailedRefunds { get; set; }

    public decimal TotalRefundAmount { get; set; }

    public decimal PendingRefundAmount { get; set; }
}