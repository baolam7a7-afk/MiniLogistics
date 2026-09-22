namespace MiniLogistics.BLL.DTOs.Cart;

public class CartItemResponseDto
{
    public long Id { get; set; }

    public long VariantId { get; set; }

    public string VariantName { get; set; } = string.Empty;

    public long ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int Quantity { get; set; }

    public decimal TotalPrice { get; set; }
}