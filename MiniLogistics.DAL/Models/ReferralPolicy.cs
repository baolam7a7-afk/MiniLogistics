namespace MiniLogistics.DAL.Models;

/// <summary>
/// Cấu hình chương trình thưởng giới thiệu mở shop. Hệ thống dùng một bản ghi đang bật.
/// </summary>
public class ReferralPolicy
{
    public long Id { get; set; }

    /// <summary>flat = tiền cố định, percent = phần trăm phí sàn.</summary>
    public string RewardMode { get; set; } = "flat";

    public decimal FlatAmount { get; set; }

    /// <summary>Phần trăm người giới thiệu được hưởng trên phí sàn.</summary>
    public decimal SharePercent { get; set; }

    /// <summary>Phí sàn tính trên tổng đơn giao thành công đầu tiên của shop mới.</summary>
    public decimal PlatformFeePercent { get; set; }

    public int MinProductCount { get; set; }

    /// <summary>Shop phải được admin duyệt (thay cho bước KYC).</summary>
    public bool RequireApprovedShop { get; set; } = true;

    public bool RequireFirstOrder { get; set; } = true;

    /// <summary>Số ngày giữ thưởng sau khi đủ điều kiện.</summary>
    public int HoldingDays { get; set; } = 14;

    /// <summary>manual = admin bấm duyệt, auto = cộng ví khi hết thời gian giữ.</summary>
    public string PayoutMode { get; set; } = "manual";

    public bool IsEnabled { get; set; } = true;

    public DateTime UpdatedAt { get; set; }
}
