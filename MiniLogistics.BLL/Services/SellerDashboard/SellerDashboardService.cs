using Microsoft.EntityFrameworkCore;

using MiniLogistics.BLL.DTOs.SellerDashboard;
using MiniLogistics.BLL.Exceptions;
using MiniLogistics.BLL.Services.Order;
using MiniLogistics.DAL.UnitOfWork;

namespace MiniLogistics.BLL.Services.SellerDashboard;

public class SellerDashboardService : ISellerDashboardService
{
    private readonly IUnitOfWork _unitOfWork;

    public SellerDashboardService(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SellerDashboardResponseDTO> GetDashboardAsync(
        long userId)
    {
        // =====================================================
        // 1. LẤY TẤT CẢ SHOP CỦA SELLER
        // =====================================================

        var shops = await _unitOfWork.Shops
            .Query()
            .AsNoTracking()
            .Where(x => x.OwnerUserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        // Seller chưa có Shop
        if (shops.Count == 0)
        {
            throw new NotFoundException(
                "Seller chưa có shop.");
        }

        // Lấy danh sách ShopId
        var shopIds = shops
            .Select(x => x.Id)
            .ToList();

        // =====================================================
        // 2. TIME
        // =====================================================

        var now = DateTime.UtcNow;

        var today = now.Date;

        var firstDayOfMonth = new DateTime(
            now.Year,
            now.Month,
            1,
            0,
            0,
            0,
            DateTimeKind.Utc);

        // =====================================================
        // 3. ORDERS - TẤT CẢ SHOP
        // =====================================================

        var orders = _unitOfWork.Orders
            .Query()
            .AsNoTracking()
            .Where(x => shopIds.Contains(x.ShopId));

        // -----------------------------------------------------
        // Tổng số Order
        // -----------------------------------------------------

        var totalOrders =
            await orders.CountAsync();

        // -----------------------------------------------------
        // Order theo Status
        // -----------------------------------------------------

        var pendingOrders =
            await orders.CountAsync(
                x => x.Status == OrderStatuses.Pending);

        var confirmedOrders =
            await orders.CountAsync(
                x => x.Status == OrderStatuses.Confirmed);

        var processingOrders =
            await orders.CountAsync(
                x => x.Status == OrderStatuses.Processing);

        var shippingOrders =
            await orders.CountAsync(
                x => x.Status == OrderStatuses.Shipping);

        var deliveredOrders =
            await orders.CountAsync(
                x => x.Status == OrderStatuses.Delivered);

        var cancelledOrders =
            await orders.CountAsync(
                x => x.Status == OrderStatuses.Cancelled);

        // =====================================================
        // 4. REVENUE - TẤT CẢ SHOP
        // =====================================================

        // Chỉ Order delivered được tính doanh thu
        var totalRevenue =
            await orders
                .Where(x =>
                    x.Status == OrderStatuses.Delivered)
                .SumAsync(x => (decimal?)x.Total)
            ?? 0m;

        // Doanh thu hôm nay
        var todayRevenue =
            await orders
                .Where(x =>
                    x.Status == OrderStatuses.Delivered &&
                    x.PlacedAt >= today)
                .SumAsync(x => (decimal?)x.Total)
            ?? 0m;

        // Doanh thu tháng này
        var thisMonthRevenue =
            await orders
                .Where(x =>
                    x.Status == OrderStatuses.Delivered &&
                    x.PlacedAt >= firstDayOfMonth)
                .SumAsync(x => (decimal?)x.Total)
            ?? 0m;

        // =====================================================
        // 5. RECENT ORDERS - 5 ORDER GẦN NHẤT
        // =====================================================

        var recentOrderRows =
            await orders
                .OrderByDescending(x => x.PlacedAt)
                .Take(5)
                .Select(x => new
                {
                    x.Id,
                    x.OrderCode,
                    x.ShopId,
                    x.Status,
                    x.Total,
                    x.PlacedAt
                })
                .ToListAsync();

        var shopDictionary = shops
            .ToDictionary(
                x => x.Id,
                x => x.Name);

        var recentOrders =
            recentOrderRows
                .Select(x => new RecentOrderDTO
                {
                    OrderId = x.Id,

                    OrderCode = x.OrderCode,

                    ShopId = x.ShopId,

                    ShopName =
                        shopDictionary.TryGetValue(
                            x.ShopId,
                            out var shopName)
                            ? shopName
                            : string.Empty,

                    Status = x.Status,

                    Total = x.Total,

                    PlacedAt = x.PlacedAt
                })
                .ToList();

        // =====================================================
        // 6. PRODUCTS - TẤT CẢ SHOP
        // =====================================================

        var products =
            _unitOfWork.Products
                .Query()
                .AsNoTracking()
                .Where(x =>
                    shopIds.Contains(x.ShopId));

        var totalProducts =
            await products.CountAsync();

        var activeProducts =
            await products.CountAsync(
                x => x.Status == "active");

        var draftProducts =
            await products.CountAsync(
                x => x.Status == "draft");

        var otherProducts =
            await products.CountAsync(
                x =>
                    x.Status != "active" &&
                    x.Status != "draft");

        // =====================================================
        // 7. PRODUCT VARIANTS
        // =====================================================

        var productIds =
            products.Select(x => x.Id);

        var variants =
            _unitOfWork.ProductVariants
                .Query()
                .AsNoTracking()
                .Where(x =>
                    productIds.Contains(
                        x.ProductId));

        var totalVariants =
            await variants.CountAsync();

        var activeVariants =
            await variants.CountAsync(
                x => x.IsActive);

        // =====================================================
        // 8. INVENTORY - TẤT CẢ SHOP
        // =====================================================

        var variantIds =
            variants.Select(x => x.Id);

        var inventories =
            _unitOfWork.Inventories
                .Query()
                .AsNoTracking()
                .Where(x =>
                    variantIds.Contains(
                        x.ProductVariantId));

        var totalInventoryQuantity =
            await inventories
                .SumAsync(x => (int?)x.Quantity)
            ?? 0;

        var totalReservedQuantity =
            await inventories
                .SumAsync(x => (int?)x.ReservedQuantity)
            ?? 0;

        var availableQuantity =
            totalInventoryQuantity -
            totalReservedQuantity;

        var outOfStockVariants =
            await inventories
                .CountAsync(
                    x =>
                        x.Quantity -
                        x.ReservedQuantity <= 0);

        // =====================================================
        // 9. SHOP SUMMARY
        // =====================================================

        var shopSummaries =
            shops
                .Select(shop => new ShopSummaryDTO
                {
                    Id = shop.Id,

                    Name = shop.Name,

                    Slug = shop.Slug,

                    Description = shop.Description,

                    LogoUrl = shop.LogoUrl,

                    Status = shop.Status,

                    ApprovedAt = shop.ApprovedAt,

                    CreatedAt = shop.CreatedAt
                })
                .ToList();

        // =====================================================
        // 10. SHOP WALLETS - TẤT CẢ SHOP
        // =====================================================

        var wallets =
            await _unitOfWork.ShopWallets
                .Query()
                .AsNoTracking()
                .Where(x =>
                    shopIds.Contains(x.ShopId))
                .ToListAsync();

        var walletIds =
            wallets
                .Select(x => x.Id)
                .ToList();

        // Tổng Balance của tất cả Shop Wallet
        var totalWalletBalance =
            wallets.Sum(x => x.Balance);

        // =====================================================
        // 11. WALLET TRANSACTIONS
        // =====================================================

        var walletTransactions =
            _unitOfWork.ShopWalletTransactions
                .Query()
                .AsNoTracking()
                .Where(x =>
                    walletIds.Contains(
                        x.WalletId));

        var transactionCount =
            await walletTransactions.CountAsync();

        // SALE_CREDIT = Credit
        var totalCredit =
            await walletTransactions
                .Where(x =>
                    x.Type == "SALE_CREDIT")
                .SumAsync(x => (decimal?)x.Amount)
            ?? 0m;

        // REFUND_DEBIT + PAYOUT_DEBIT = Debit
        var totalDebit =
            await walletTransactions
                .Where(x =>
                    x.Type == "REFUND_DEBIT" ||
                    x.Type == "PAYOUT_DEBIT")
                .SumAsync(x => (decimal?)x.Amount)
            ?? 0m;

        // =====================================================
        // 12. REFUND
        // =====================================================

        // Lấy OrderId của tất cả Shop
        var shopOrderIds =
            orders.Select(x => x.Id);

        // ReturnRequest thuộc các Order của Seller
        var shopReturnRequests =
            _unitOfWork.ReturnRequests
                .Query()
                .AsNoTracking()
                .Where(x =>
                    shopOrderIds.Contains(
                        x.OrderId));

        // Lấy ReturnRequestId
        var shopReturnRequestIds =
            shopReturnRequests.Select(x => x.Id);

        // Refund thuộc các ReturnRequest trên
        var refunds =
            _unitOfWork.RefundTransactions
                .Query()
                .AsNoTracking()
                .Where(x =>
                    shopReturnRequestIds.Contains(
                        x.ReturnRequestId));

        var totalRefunds =
            await refunds.CountAsync();

        var pendingRefunds =
            await refunds.CountAsync(
                x => x.Status == "pending");

        var completedRefunds =
            await refunds.CountAsync(
                x => x.Status == "done");

        var failedRefunds =
            await refunds.CountAsync(
                x => x.Status == "failed");

        var totalRefundAmount =
            await refunds
                .SumAsync(x => (decimal?)x.Amount)
            ?? 0m;

        var pendingRefundAmount =
            await refunds
                .Where(x => x.Status == "pending")
                .SumAsync(x => (decimal?)x.Amount)
            ?? 0m;

        // =====================================================
        // 13. PAYOUT - TẤT CẢ SHOP
        // =====================================================

        var payouts =
            _unitOfWork.PayoutRequests
                .Query()
                .AsNoTracking()
                .Where(x =>
                    shopIds.Contains(
                        x.ShopId));

        var totalPayoutRequests =
            await payouts.CountAsync();

        var requestedPayoutRequests =
            await payouts.CountAsync(
                x => x.Status == "requested");

        var processedPayoutRequests =
            await payouts.CountAsync(
                x => x.ProcessedAt != null);

        var totalPayoutAmount =
            await payouts
                .SumAsync(x => (decimal?)x.Amount)
            ?? 0m;

        var requestedPayoutAmount =
            await payouts
                .Where(x => x.Status == "requested")
                .SumAsync(x => (decimal?)x.Amount)
            ?? 0m;

        var processedPayoutAmount =
            await payouts
                .Where(x => x.ProcessedAt != null)
                .SumAsync(x => (decimal?)x.Amount)
            ?? 0m;

        // =====================================================
        // 14. RETURN DASHBOARD
        // =====================================================

        return new SellerDashboardResponseDTO
        {
            SellerId = userId,

            TotalShops = shops.Count,

            Shops = shopSummaries,

            Revenue = new RevenueSummaryDTO
            {
                TotalRevenue = totalRevenue,

                TodayRevenue = todayRevenue,

                ThisMonthRevenue = thisMonthRevenue,

                TotalOrders = totalOrders,

                DeliveredOrders = deliveredOrders,

                PendingOrders = pendingOrders,

                CancelledOrders = cancelledOrders
            },

            Orders = new OrderSummaryDTO
            {
                Total = totalOrders,

                Pending = pendingOrders,

                Confirmed = confirmedOrders,

                Processing = processingOrders,

                Shipping = shippingOrders,

                Delivered = deliveredOrders,

                Cancelled = cancelledOrders,

                RecentOrders = recentOrders
            },

            Products = new ProductSummaryDTO
            {
                TotalProducts = totalProducts,

                ActiveProducts = activeProducts,

                DraftProducts = draftProducts,

                OtherProducts = otherProducts,

                TotalVariants = totalVariants,

                ActiveVariants = activeVariants
            },

            Inventory = new InventorySummaryDTO
            {
                TotalVariants = totalVariants,

                OutOfStockVariants =
                    outOfStockVariants,

                TotalQuantity =
                    totalInventoryQuantity,

                ReservedQuantity =
                    totalReservedQuantity,

                AvailableQuantity =
                    availableQuantity
            },

            Wallet = new WalletSummaryDTO
            {
                HasWallet = wallets.Count > 0,

                Balance = totalWalletBalance,

                TransactionCount =
                    transactionCount,

                TotalCredit =
                    totalCredit,

                TotalDebit =
                    totalDebit
            },

            Refunds = new RefundSummaryDTO
            {
                TotalRefunds =
                    totalRefunds,

                PendingRefunds =
                    pendingRefunds,

                CompletedRefunds =
                    completedRefunds,

                FailedRefunds =
                    failedRefunds,

                TotalRefundAmount =
                    totalRefundAmount,

                PendingRefundAmount =
                    pendingRefundAmount
            },

            Payouts = new PayoutSummaryDTO
            {
                TotalRequests =
                    totalPayoutRequests,

                RequestedRequests =
                    requestedPayoutRequests,

                ProcessedRequests =
                    processedPayoutRequests,

                TotalAmount =
                    totalPayoutAmount,

                RequestedAmount =
                    requestedPayoutAmount,

                ProcessedAmount =
                    processedPayoutAmount
            }
        };
    }
}