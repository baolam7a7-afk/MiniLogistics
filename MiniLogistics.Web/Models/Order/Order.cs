namespace MiniLogistics.Web.Models.Order;

public class Order
{
    public long Id { get; set; }

    public string OrderCode { get; set; } = string.Empty;

    public long CustomerId { get; set; }

    public long ShopId { get; set; }

    public long ShippingAddressId { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Currency { get; set; } = string.Empty;

    public decimal Subtotal { get; set; }

    public decimal ShippingFee { get; set; }

    public decimal DiscountTotal { get; set; }

    public decimal Total { get; set; }

    public string PaymentMethod { get; set; } = string.Empty;

    public string? Note { get; set; }

    public DateTime PlacedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public List<OrderItem> Items { get; set; } = new();

    public List<OrderStatusLog> StatusLogs { get; set; } = new();
}