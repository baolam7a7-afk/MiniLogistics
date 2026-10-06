namespace MiniLogistics.DAL.Models;

/// <summary>
/// Lịch sử tiền thưởng giới thiệu, gắn với một giao dịch ví shop khi đã chi.
/// </summary>
public class ReferralRewardTransaction
{
    public long Id { get; set; }
    public long SellerReferralId { get; set; }
    public long? ShopWalletTransactionId { get; set; }
    public decimal Amount { get; set; }

    /// <summary>held = đang giữ, paid = đã cộng ví, cancelled = không chi.</summary>
    public string Status { get; set; } = "held";

    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }

    public SellerReferral SellerReferral { get; set; } = null!;
    public ShopWalletTransaction? ShopWalletTransaction { get; set; }
}
