using System.ComponentModel.DataAnnotations;

namespace MiniLogistics.BLL.DTOs.ProductVariant;

public class CreateProductVariantDTO
{
    [Required]
    public long ProductId { get; set; }

    [MaxLength(100)]
    public string? Sku { get; set; }

    [Required]
    [MaxLength(300)]
    public string VariantName { get; set; } = string.Empty;

    public string? AttributesJson { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    public bool IsActive { get; set; } = true;
}