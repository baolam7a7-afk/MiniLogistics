namespace MiniLogistics.Web.Models.Admin;

public class AdminDashboard
{
    public AdminOverview Overview { get; set; } = new();
    public List<DailyStat> ByDay { get; set; } = new();
    public List<MonthlyStat> ByMonth { get; set; } = new();
    public List<ShopStat> ByShop { get; set; } = new();
    public List<SellerStat> BySeller { get; set; } = new();
    public List<OrderStatusStat> ByOrderStatus { get; set; } = new();
}

public class AdminOverview
{
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public int TodayOrders { get; set; }
    public int ThisMonthOrders { get; set; }
    public int TotalUsers { get; set; }
    public int TotalSellers { get; set; }
    public int TotalShops { get; set; }
    public int TotalProducts { get; set; }
    public int TotalRefunds { get; set; }
    public decimal TotalRefundAmount { get; set; }
}

public class DailyStat
{
    public DateTime Date { get; set; }
    public int TotalOrders { get; set; }
    public decimal Revenue { get; set; }
}

public class MonthlyStat
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int TotalOrders { get; set; }
    public decimal Revenue { get; set; }
}

public class ShopStat
{
    public long ShopId { get; set; }
    public string ShopName { get; set; } = "";
    public long SellerId { get; set; }
    public string SellerName { get; set; } = "";
    public int TotalOrders { get; set; }
    public int DeliveredOrders { get; set; }
    public decimal Revenue { get; set; }
}

public class SellerStat
{
    public long SellerId { get; set; }
    public string SellerName { get; set; } = "";
    public string Email { get; set; } = "";
    public int TotalShops { get; set; }
    public int TotalOrders { get; set; }
    public int DeliveredOrders { get; set; }
    public decimal Revenue { get; set; }
}

public class OrderStatusStat
{
    public string Status { get; set; } = "";
    public int TotalOrders { get; set; }
    public decimal TotalAmount { get; set; }
}

public class CategoryRecord
{
    public long Id { get; set; }
    public long? ParentId { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PaymentRecord
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public string OrderCode { get; set; } = "";
    public long CustomerId { get; set; }
    public long ShopId { get; set; }
    public string? Provider { get; set; }
    public string Method { get; set; } = "";
    public decimal Amount { get; set; }
    public string Status { get; set; } = "";
    public string? ProviderTxnId { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RefundRecord
{
    public long Id { get; set; }
    public long ReturnRequestId { get; set; }
    public long OrderId { get; set; }
    public string OrderCode { get; set; } = "";
    public long CustomerId { get; set; }
    public string? CustomerEmail { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class DisputeRecord
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public long RaisedByUserId { get; set; }
    public string Reason { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public long? HandledByUserId { get; set; }
    public DateTime? HandledAt { get; set; }
}

public class DisputeMessageRecord
{
    public long Id { get; set; }
    public long DisputeId { get; set; }
    public long SenderUserId { get; set; }
    public string Message { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class ReportRecord
{
    public long Id { get; set; }
    public string Scope { get; set; } = "";
    public long? ShopId { get; set; }
    public DateOnly DateFrom { get; set; }
    public DateOnly DateTo { get; set; }
    public string MetricsJson { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public static class AdminText
{
    public static string UserStatus(string? status) => status?.Trim().ToLowerInvariant() switch
    {
        "active" => "Đang hoạt động",
        "locked" => "Đã khóa",
        _ => string.IsNullOrWhiteSpace(status) ? "—" : status
    };

    public static string PaymentStatus(string? status) => status?.Trim().ToLowerInvariant() switch
    {
        "pending" => "Chờ thanh toán",
        "paid" => "Đã thanh toán",
        "failed" => "Thất bại",
        "expired" => "Hết hạn",
        _ => string.IsNullOrWhiteSpace(status) ? "—" : status
    };

    public static string RefundStatus(string? status) => status?.Trim().ToLowerInvariant() switch
    {
        "pending" => "Chờ xử lý",
        "done" => "Đã hoàn",
        "failed" => "Thất bại",
        _ => string.IsNullOrWhiteSpace(status) ? "—" : status
    };

    public static string DisputeStatus(string? status) => status?.Trim().ToLowerInvariant() switch
    {
        "open" => "Mở",
        "in_progress" => "Đang xử lý",
        "resolved" => "Đã giải quyết",
        "rejected" => "Từ chối",
        "closed" => "Đã đóng",
        _ => string.IsNullOrWhiteSpace(status) ? "—" : status
    };

    public static string WalletType(string? type) => type?.Trim().ToUpperInvariant() switch
    {
        "SALE_CREDIT" => "Cộng doanh thu",
        "REFUND_DEBIT" => "Trừ hoàn tiền",
        "PAYOUT_DEBIT" => "Trừ rút tiền",
        "REFERRAL" => "Hoa hồng giới thiệu",
        "ADJUSTMENT" => "Điều chỉnh",
        _ => string.IsNullOrWhiteSpace(type) ? "—" : type
    };

    public static IReadOnlyList<string> NextOrderStatuses(string? current) =>
        current?.Trim().ToLowerInvariant() switch
        {
            "pending" or "paid" => new[] { "confirmed", "cancelled" },
            _ => Array.Empty<string>()
        };
}
