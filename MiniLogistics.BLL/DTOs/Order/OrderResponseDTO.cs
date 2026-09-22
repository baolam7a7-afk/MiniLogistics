namespace MiniLogistics.BLL.DTOs.Order;

public class OrderResponseDTO
{
    public long Id { get; set; }

    public string OrderCode { get; set; } = string.Empty;

    public long CustomerId { get; set; }

    public long ShopId { get; set; }

    public long ShippingAddressId { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Currency { get; set; } = "VND";

    public decimal Subtotal { get; set; }

    public decimal ShippingFee { get; set; }

    public decimal DiscountTotal { get; set; }

    public decimal Total { get; set; }

    public string PaymentMethod { get; set; } = string.Empty;

    public string? Note { get; set; }

    public DateTime PlacedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }


    // ==========================================
    // ITEMS
    // ==========================================

    public List<OrderItemResponseDTO> Items { get; set; }
        = new();


    // ==========================================
    // STATUS HISTORY
    // ==========================================

    public List<OrderStatusLogResponseDTO> StatusLogs { get; set; }
        = new();
}