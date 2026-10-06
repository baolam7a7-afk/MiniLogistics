using System.ComponentModel.DataAnnotations;

namespace MiniLogistics.BLL.DTOs.Order;

public class CancelOrderDTO
{
    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}
