namespace MiniLogistics.Web.Models.Cart;

public class CartItem
{
    public long Id { get; set; }

    public long VariantId { get; set; }

    public long ProductId { get; set; }

    public long ShopId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string VariantName { get; set; } = string.Empty;

    public string Sku { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public decimal Price { get; set; }

    public int Quantity { get; set; }

    public decimal TotalPrice { get; set; }

    public int AvailableQuantity { get; set; }
}