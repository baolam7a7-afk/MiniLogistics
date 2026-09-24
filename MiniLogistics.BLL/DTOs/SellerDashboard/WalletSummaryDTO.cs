namespace MiniLogistics.BLL.DTOs.SellerDashboard;

public class WalletSummaryDTO
{
    public bool HasWallet { get; set; }

    public decimal Balance { get; set; }

    public int TransactionCount { get; set; }

    public decimal TotalCredit { get; set; }

    public decimal TotalDebit { get; set; }
}