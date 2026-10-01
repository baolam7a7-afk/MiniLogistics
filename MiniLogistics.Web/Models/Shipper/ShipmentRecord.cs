namespace MiniLogistics.Web.Models.Shipper;

public class ShipmentRecord
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public long? ShipperUserId { get; set; }
    public string? ShipperName { get; set; }
    public string? TrackingCode { get; set; }
    public string Status { get; set; } = "";
    public decimal CodAmount { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? PickedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<ShipmentEventRecord> Events { get; set; } = new();
    public string? OrderCode { get; set; }
    public string? ShopName { get; set; }
    public string? PaymentMethod { get; set; }
    public decimal ShippingFee { get; set; }
    public string? Note { get; set; }
    public string? ReceiverName { get; set; }
    public string? ReceiverPhone { get; set; }
    public string? AddressLine { get; set; }
    public List<ShipmentLineRecord> Items { get; set; } = new();
}

public class ShipmentLineRecord
{
    public string ProductName { get; set; } = "";
    public string VariantName { get; set; } = "";
    public int Quantity { get; set; }
}

public class ShipmentEventRecord
{
    public long Id { get; set; }
    public string Status { get; set; } = "";
    public string? Location { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}

public static class ShipperText
{
    public static string Status(string? status) => status?.Trim().ToLowerInvariant() switch
    {
        "created" => "Mới tạo",
        "assigned" => "Chờ lấy hàng",
        "picked_up" => "Đã lấy hàng",
        "shipping" => "Đang giao",
        "delivered" => "Đã giao",
        _ => string.IsNullOrWhiteSpace(status) ? "—" : status
    };

    public static string? Next(string? status) => status?.Trim().ToLowerInvariant() switch
    {
        "assigned" => "picked_up",
        "picked_up" => "shipping",
        "shipping" => "delivered",
        _ => null
    };

    public static string Action(string? next) => next switch
    {
        "picked_up" => "Xác nhận đã lấy hàng",
        "shipping" => "Bắt đầu giao",
        "delivered" => "Xác nhận giao thành công",
        _ => "Cập nhật"
    };

    public static string Badge(string? status) => status?.Trim().ToLowerInvariant() switch
    {
        "delivered" => "ok",
        "shipping" => "warn",
        "assigned" => "bad",
        _ => ""
    };
}
