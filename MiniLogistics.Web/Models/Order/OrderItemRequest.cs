namespace MiniLogistics.Web.Models.Order;

public class OrderItemRequest
{
    public long VariantId { get; set; }

    public int Quantity { get; set; }
}