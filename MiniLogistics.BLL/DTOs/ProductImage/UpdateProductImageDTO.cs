using System.ComponentModel.DataAnnotations;

namespace MiniLogistics.BLL.DTOs.ProductImage;

public class UpdateProductImageDTO
{
    [Required]
    [MaxLength(1000)]
    public string Url { get; set; } = null!;
}