namespace MiniLogistics.DAL.Models;

public class UserVoucher
{
    public long Id { get; set; }

    public long UserId { get; set; }

    public long VoucherId { get; set; }

    public DateTime ClaimedAt { get; set; }

    /// <summary>available | used | expired</summary>
    public string Status { get; set; } = "available";

    public DateTime? UsedAt { get; set; }

    public long? UsedOrderId { get; set; }

    public User User { get; set; } = null!;

    public Voucher Voucher { get; set; } = null!;
}
