using System.ComponentModel.DataAnnotations;

namespace MiniLogistics.BLL.DTOs.Order;

public class CreateOrderDTO
{
    [Required]
    public long ShippingAddressId { get; set; }


    [Required]
    [MaxLength(20)]
    public string PaymentMethod { get; set; } = "cod";


    [MaxLength(1000)]
    public string? Note { get; set; }


    [Required]
    [MinLength(1)]
    public List<CreateOrderItemDTO> Items { get; set; }
        = new();
}