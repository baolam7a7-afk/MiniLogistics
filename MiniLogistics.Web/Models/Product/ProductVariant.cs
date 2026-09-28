namespace MiniLogistics.Web.Models.Product;

public class ProductVariant
{
    public long Id { get; set; }

    public long ProductId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string VariantName { get; set; } = string.Empty;

    public string AttributesJson { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int Stock { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}