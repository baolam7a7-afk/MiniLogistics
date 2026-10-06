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

    public string? CustomerName { get; set; }

    public string? CustomerPhone { get; set; }

    public string? ShopName { get; set; }

    public string? ReceiverName { get; set; }

    public string? ReceiverPhone { get; set; }

    public string? ShippingAddressText { get; set; }

    public string? CancelReason { get; set; }

    public DateTime PlacedAt { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public DateTime? CustomerConfirmedAt { get; set; }

    public string? ShipperName { get; set; }

    public DateTime? ShipperAcceptedAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public List<OrderItem> Items { get; set; } = new();

    public List<OrderStatusLog> StatusLogs { get; set; } = new();
}