using System.ComponentModel.DataAnnotations;

namespace MiniLogistics.BLL.DTOs.Order;

public class UpdateOrderStatusDTO
{
    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = string.Empty;


    [MaxLength(500)]
    public string? Message { get; set; }
}