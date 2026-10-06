namespace MiniLogistics.DAL.Models;

/// <summary>
/// Một lượt giới thiệu: người có mã dẫn một shop mới.
/// </summary>
public class SellerReferral
{
    public long Id { get; set; }
    public long ReferrerUserId { get; set; }
    public long ReferredUserId { get; set; }
    public long ReferredShopId { get; set; }
    public string Code { get; set; } = null!;

    /// <summary>pending, approved, rejected, flagged.</summary>
    public string Status { get; set; } = "pending";

    public string? SignupIp { get; set; }
    public string? DeviceHint { get; set; }
    public string? FraudFlags { get; set; }
    public decimal RewardAmount { get; set; }
    public DateTime? QualifiedAt { get; set; }
    public DateTime? PayableAt { get; set; }
    public long? ReviewedByUserId { get; set; }
    public string? ReviewNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public User ReferrerUser { get; set; } = null!;
    public User ReferredUser { get; set; } = null!;
    public Shop ReferredShop { get; set; } = null!;
    public User? ReviewedByUser { get; set; }
    public ICollection<ReferralRewardTransaction> Rewards { get; set; } = new List<ReferralRewardTransaction>();
}
