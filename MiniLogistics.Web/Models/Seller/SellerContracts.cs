namespace MiniLogistics.Web.Models.Seller;

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
}

public class ShopRecord
{
    public long Id { get; set; }
    public long OwnerUserId { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string Status { get; set; } = "";
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class SellerDashboard
{
    public long SellerId { get; set; }
    public int TotalShops { get; set; }
    public List<ShopSummary> Shops { get; set; } = new();
    public RevenueSummary Revenue { get; set; } = new();
    public OrderSummary Orders { get; set; } = new();
    public ProductSummary Products { get; set; } = new();
    public InventorySummary Inventory { get; set; } = new();
    public WalletSummary Wallet { get; set; } = new();
    public RefundSummary Refunds { get; set; } = new();
    public PayoutSummary Payouts { get; set; } = new();
}

public class ShopSummary
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string Status { get; set; } = "";
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RevenueSummary
{
    public decimal TotalRevenue { get; set; }
    public decimal TodayRevenue { get; set; }
    public decimal ThisMonthRevenue { get; set; }
    public int TotalOrders { get; set; }
    public int DeliveredOrders { get; set; }
    public int PendingOrders { get; set; }
    public int CancelledOrders { get; set; }
}

public class OrderSummary
{
    public int Total { get; set; }
    public int Pending { get; set; }
    public int Confirmed { get; set; }
    public int Processing { get; set; }
    public int Shipping { get; set; }
    public int Delivered { get; set; }
    public int Cancelled { get; set; }
    public List<RecentOrder> RecentOrders { get; set; } = new();
}

public class RecentOrder
{
    public long OrderId { get; set; }
    public string OrderCode { get; set; } = "";
    public long ShopId { get; set; }
    public string ShopName { get; set; } = "";
    public string Status { get; set; } = "";
    public decimal Total { get; set; }
    public DateTime PlacedAt { get; set; }
}

public class ProductSummary
{
    public int TotalProducts { get; set; }
    public int ActiveProducts { get; set; }
    public int DraftProducts { get; set; }
    public int OtherProducts { get; set; }
    public int TotalVariants { get; set; }
    public int ActiveVariants { get; set; }
}

public class InventorySummary
{
    public int TotalVariants { get; set; }
    public int OutOfStockVariants { get; set; }
    public int TotalQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int AvailableQuantity { get; set; }
}

public class WalletSummary
{
    public bool HasWallet { get; set; }
    public decimal Balance { get; set; }
    public int TransactionCount { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal TotalDebit { get; set; }
}

public class RefundSummary
{
    public int TotalRefunds { get; set; }
    public int PendingRefunds { get; set; }
    public int CompletedRefunds { get; set; }
    public int FailedRefunds { get; set; }
    public decimal TotalRefundAmount { get; set; }
    public decimal PendingRefundAmount { get; set; }
}

public class PayoutSummary
{
    public int TotalRequests { get; set; }
    public int RequestedRequests { get; set; }
    public int ProcessedRequests { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal RequestedAmount { get; set; }
    public decimal ProcessedAmount { get; set; }
}

public class SellerProduct
{
    public long Id { get; set; }
    public long ShopId { get; set; }
    public long CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Description { get; set; }
    public string Status { get; set; } = "";
    public string? ImageUrl { get; set; }
    public decimal? MinPrice { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class SellerVariant
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public string? Sku { get; set; }
    public string VariantName { get; set; } = "";
    public string? AttributesJson { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SellerProductImage
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public string Url { get; set; } = "";
}

public class SellerOrder
{
    public long Id { get; set; }
    public string OrderCode { get; set; } = "";
    public long CustomerId { get; set; }
    public long ShopId { get; set; }
    public long ShippingAddressId { get; set; }
    public string Status { get; set; } = "";
    public string Currency { get; set; } = "VND";
    public decimal Subtotal { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal Total { get; set; }
    public string PaymentMethod { get; set; } = "";
    public string? Note { get; set; }
    public DateTime PlacedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<SellerOrderItem> Items { get; set; } = new();
    public List<SellerOrderLog> StatusLogs { get; set; } = new();
}

public class SellerOrderItem
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public long VariantId { get; set; }
    public string ProductName { get; set; } = "";
    public string VariantName { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
}

public class SellerOrderLog
{
    public long Id { get; set; }
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = "";
    public string? Message { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SellerVoucher
{
    public long Id { get; set; }
    public string Scope { get; set; } = "";
    public long? ShopId { get; set; }
    public string Code { get; set; } = "";
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string DiscountType { get; set; } = "";
    public decimal DiscountValue { get; set; }
    public decimal? MaxDiscount { get; set; }
    public decimal? MinOrderValue { get; set; }
    public int? UsageLimit { get; set; }
    public int UsedCount { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class ShopWalletRecord
{
    public long Id { get; set; }
    public long ShopId { get; set; }
    public string ShopName { get; set; } = "";
    public long OwnerUserId { get; set; }
    public decimal Balance { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class WalletTransaction
{
    public long Id { get; set; }
    public long WalletId { get; set; }
    public long ShopId { get; set; }
    public string ShopName { get; set; } = "";
    public long? OrderId { get; set; }
    public string Type { get; set; } = "";
    public decimal Amount { get; set; }
    public decimal? BalanceAfter { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PayoutRecord
{
    public long Id { get; set; }
    public long ShopId { get; set; }
    public string? ShopName { get; set; }
    public decimal Amount { get; set; }
    public string BankAccountName { get; set; } = "";
    public string BankAccountNumber { get; set; } = "";
    public string BankName { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime RequestedAt { get; set; }
    public long? ProcessedByUserId { get; set; }
    public DateTime? ProcessedAt { get; set; }
}

public class ReturnRecord
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public string OrderCode { get; set; } = "";
    public long CustomerId { get; set; }
    public string CustomerEmail { get; set; } = "";
    public string Reason { get; set; } = "";
    public string? Description { get; set; }
    public string Status { get; set; } = "";
    public DateTime RequestedAt { get; set; }
    public long? HandledByUserId { get; set; }
    public string? HandledByUserEmail { get; set; }
    public DateTime? HandledAt { get; set; }
}

public class SellerReview
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public long OrderItemId { get; set; }
    public long ProductId { get; set; }
    public long CustomerId { get; set; }
    public int Rating { get; set; }
    public string? Content { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ReviewReplyRecord
{
    public long Id { get; set; }
    public long ReviewId { get; set; }
    public long ShopId { get; set; }
    public long RepliedByUserId { get; set; }
    public string Content { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class SupportTicketRecord
{
    public long Id { get; set; }
    public string Subject { get; set; } = "";
    public string Status { get; set; } = "";
    public long CreatedByUserId { get; set; }
    public long? OrderId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SupportMessageRecord
{
    public long Id { get; set; }
    public long TicketId { get; set; }
    public long SenderUserId { get; set; }
    public string Message { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
