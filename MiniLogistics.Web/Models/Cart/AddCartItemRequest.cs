namespace MiniLogistics.Web.Models.Cart;

public class AddCartItemRequest
{
    public long VariantId { get; set; }

    public int Quantity { get; set; }
}