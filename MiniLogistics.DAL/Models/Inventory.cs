namespace MiniLogistics.DAL.Models;

public class Inventory
{
    public long Id { get; set; }

    public long ProductVariantId { get; set; }

    public int Quantity { get; set; }

    public int ReservedQuantity { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }


    // =====================================================
    // RELATIONSHIP
    // =====================================================

    public ProductVariant ProductVariant { get; set; } = null!;
}