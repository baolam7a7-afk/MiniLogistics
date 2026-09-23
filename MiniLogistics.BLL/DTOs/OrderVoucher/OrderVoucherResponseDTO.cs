namespace MiniLogistics.BLL.DTOs.OrderVoucher;

public class OrderVoucherResponseDTO
{
    public long Id { get; set; }

    public long OrderId { get; set; }

    public long VoucherId { get; set; }

    public string CodeSnapshot { get; set; } = null!;

    public decimal DiscountAmount { get; set; }
}