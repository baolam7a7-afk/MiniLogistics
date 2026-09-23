using System.ComponentModel.DataAnnotations;

namespace MiniLogistics.BLL.DTOs.Voucher;

public class UpdateVoucherDTO
{
    [MaxLength(20)]
    public string? Scope { get; set; }

    public long? ShopId { get; set; }

    [MaxLength(100)]
    public string? Code { get; set; }

    [MaxLength(200)]
    public string? Name { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(20)]
    public string? DiscountType { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal? DiscountValue { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? MaxDiscount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? MinOrderValue { get; set; }

    [Range(1, int.MaxValue)]
    public int? UsageLimit { get; set; }

    public DateTime? StartAt { get; set; }

    public DateTime? EndAt { get; set; }

    [MaxLength(20)]
    public string? Status { get; set; }
}