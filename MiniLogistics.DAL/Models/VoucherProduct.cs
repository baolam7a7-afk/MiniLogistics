namespace MiniLogistics.DAL.Models;

public class VoucherProduct
{
    public long VoucherId { get; set; }
    public long ProductId { get; set; }
    public Voucher Voucher { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
