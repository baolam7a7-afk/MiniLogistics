namespace MiniLogistics.BLL.DTOs.Cart;

public class CartResponseDTO
{
    public long CartId { get; set; }

    public long UserId { get; set; }

    public List<CartItemResponseDTO> Items { get; set; } = new();

    public decimal TotalAmount { get; set; }
}