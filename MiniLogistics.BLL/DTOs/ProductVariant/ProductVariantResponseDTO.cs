namespace MiniLogistics.BLL.DTOs.ProductVariant;

public class ProductVariantResponseDTO
{
    public long Id { get; set; }

    public long ProductId { get; set; }

    public string? Sku { get; set; }

    public string VariantName { get; set; } = string.Empty;

    public string? AttributesJson { get; set; }

    public decimal Price { get; set; }

    // =====================================================
    // STOCK HIỂN THỊ
    // =====================================================
    // Không lưu trong ProductVariant.
    // Giá trị này được tính từ Inventory:
    //
    // Stock = Quantity - ReservedQuantity
    //
    public int Stock { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}