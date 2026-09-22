using System.ComponentModel.DataAnnotations;

namespace MiniLogistics.BLL.DTOs.Order;

public class CreateOrderItemDTO
{
    [Required]
    public long VariantId { get; set; }


    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}