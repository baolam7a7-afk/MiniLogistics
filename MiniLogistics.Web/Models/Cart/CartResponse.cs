namespace MiniLogistics.Web.Models.Cart;

public class CartResponse
{
    public long CartId { get; set; }

    public long UserId { get; set; }

    public List<CartItem> Items { get; set; } = new();

    public decimal TotalAmount { get; set; }
}