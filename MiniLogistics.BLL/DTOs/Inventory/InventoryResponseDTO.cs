namespace MiniLogistics.BLL.DTOs.Inventory;

public class InventoryResponseDTO
{
    public long Id { get; set; }

    public long ProductVariantId { get; set; }

    public string? Sku { get; set; }

    public string VariantName { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int Quantity { get; set; }

    public int ReservedQuantity { get; set; }

    public int AvailableQuantity { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}