using System.ComponentModel.DataAnnotations;

namespace MiniLogistics.BLL.DTOs.OrderVoucher;

public class ApplyVoucherDTO
{
    [Required]
    public long OrderId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Code { get; set; } = null!;
}