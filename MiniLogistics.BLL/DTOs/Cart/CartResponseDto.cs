namespace MiniLogistics.BLL.DTOs.Cart;

public class CartResponseDto
{
    public long Id { get; set; }

    public long UserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public List<CartItemResponseDto> Items { get; set; }
        = new();

    public decimal TotalAmount { get; set; }

    public int TotalItems { get; set; }
}