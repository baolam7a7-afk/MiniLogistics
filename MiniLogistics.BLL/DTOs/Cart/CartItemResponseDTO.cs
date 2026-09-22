namespace MiniLogistics.BLL.DTOs.Cart;

public class CartItemResponseDTO
{
    public long Id { get; set; }

    public long VariantId { get; set; }

    public long ProductId { get; set; }

    public string ProductName { get; set; } = null!;

    public string VariantName { get; set; } = null!;

    public string? Sku { get; set; }

    public decimal Price { get; set; }

    public int Quantity { get; set; }

    public decimal TotalPrice { get; set; }

    public int AvailableQuantity { get; set; }
}