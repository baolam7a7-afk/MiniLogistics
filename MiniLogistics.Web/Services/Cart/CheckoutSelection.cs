namespace MiniLogistics.Web.Services.Cart;

public class CheckoutSelection
{
    public HashSet<long> CartItemIds { get; } = new();
}
