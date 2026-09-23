using System.ComponentModel.DataAnnotations;

namespace MiniLogistics.BLL.DTOs.ProductImage;

public class CreateProductImageDTO
{
    [Required]
    public long ProductId { get; set; }

    [Required]
    [MaxLength(1000)]
    public string Url { get; set; } = null!;
}