using System.ComponentModel.DataAnnotations;

namespace MiniLogistics.BLL.DTOs.Voucher;

public class CreateVoucherDTO
{
    [Required]
    [MaxLength(20)]
    public string Scope { get; set; } = null!;

    public long? ShopId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Code { get; set; } = null!;

    [MaxLength(200)]
    public string? Name { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(20)]
    public string DiscountType { get; set; } = null!;

    [Range(0.01, double.MaxValue)]
    public decimal DiscountValue { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? MaxDiscount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? MinOrderValue { get; set; }

    [Range(1, int.MaxValue)]
    public int? UsageLimit { get; set; }

    [Required]
    public DateTime StartAt { get; set; }

    [Required]
    public DateTime EndAt { get; set; }
}