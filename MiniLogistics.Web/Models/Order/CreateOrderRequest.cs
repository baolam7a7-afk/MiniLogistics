namespace MiniLogistics.Web.Models.Order;

public class CreateOrderRequest
{
    public long ShippingAddressId { get; set; }

    public string PaymentMethod { get; set; } = "cod";

    public string Note { get; set; } = string.Empty;

    public List<OrderItemRequest> Items { get; set; } = new();
}