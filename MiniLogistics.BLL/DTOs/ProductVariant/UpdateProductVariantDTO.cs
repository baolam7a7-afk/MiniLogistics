namespace MiniLogistics.BLL.DTOs.ProductVariant;

public class UpdateProductVariantDTO
{
    public string? Sku { get; set; }

    public string VariantName { get; set; } = string.Empty;

    public string? AttributesJson { get; set; }

    public decimal Price { get; set; }

    public int Stock { get; set; }

    public bool IsActive { get; set; }
}