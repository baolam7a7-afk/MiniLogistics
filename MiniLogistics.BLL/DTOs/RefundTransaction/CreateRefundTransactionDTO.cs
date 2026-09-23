using System.ComponentModel.DataAnnotations;

namespace MiniLogistics.BLL.DTOs.RefundTransaction;

public class CreateRefundTransactionDTO
{
    [Required]
    public long ReturnRequestId { get; set; }

    [Required]
    [Range(typeof(decimal), "0.01", "9999999999999999")]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(20)]
    public string Method { get; set; } = null!;
}