using Microsoft.EntityFrameworkCore;

using MiniLogistics.BLL.DTOs.AdminDashboard;
using MiniLogistics.BLL.Services.Order;
using MiniLogistics.DAL.UnitOfWork;

namespace MiniLogistics.BLL.Services.AdminDashboard;

public class AdminDashboardService : IAdminDashboardService
{
    private readonly IUnitOfWork _unitOfWork;

    public AdminDashboardService(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AdminDashboardResponseDTO> GetDashboardAsync()
    {
        // =========================================================
        // 1. TIME RANGE
        // =========================================================

        var now = DateTime.UtcNow;

        var today = now.Date;

        var tomorrow = today.AddDays(1);

        var firstDayOfMonth =
            new DateTime(
                now.Year,
                now.Month,
                1);

        var firstDayOfNextMonth =
            firstDayOfMonth.AddMonths(1);

        // 7 ngày gần nhất, bao gồm hôm nay
        var firstDayOfDailyStatistics =
            today.AddDays(-6);

        // 12 tháng gần nhất, bao gồm tháng hiện tại
        var firstMonthOfStatistics =
            firstDayOfMonth.AddMonths(-11);


        // =========================================================
        // 2. BASE QUERIES
        // =========================================================

        var users =
            _unitOfWork.Users
                .Query()
                .AsNoTracking();

        var roles =
            _unitOfWork.Roles
                .Query()
                .AsNoTracking();

        var userRoles =
            _unitOfWork.UserRoles
                .Query()
                .AsNoTracking();

        var shops =
            _unitOfWork.Shops
                .Query()
                .AsNoTracking();

        var products =
            _unitOfWork.Products
                .Query()
                .AsNoTracking();

        var orders =
            _unitOfWork.Orders
                .Query()
                .AsNoTracking();

        var refunds =
            _unitOfWork.RefundTransactions
                .Query()
                .AsNoTracking();


        // =========================================================
        // 3. OVERVIEW
        // =========================================================

        var totalUsers =
            await users.CountAsync();

        var sellerUserIds =
            from userRole in userRoles
            join role in roles
                on userRole.RoleId equals role.Id
            where role.Name == "seller"
            select userRole.UserId;

        var totalSellers =
            await sellerUserIds
                .Distinct()
                .CountAsync();

        var totalShops =
            await shops.CountAsync();

        var totalProducts =
            await products.CountAsync();

        var totalOrders =
            await orders.CountAsync();

        var todayOrders =
            await orders.CountAsync(
                x =>
                    x.PlacedAt >= today &&
                    x.PlacedAt < tomorrow);

        var thisMonthOrders =
            await orders.CountAsync(
                x =>
                    x.PlacedAt >= firstDayOfMonth &&
                    x.PlacedAt < firstDayOfNextMonth);

        var totalRevenue =
            await orders
                .Where(x =>
                    x.Status == OrderStatuses.Delivered)
                .SumAsync(x => (decimal?)x.Total)
            ?? 0m;

        var totalRefunds =
            await refunds.CountAsync();

        var totalRefundAmount =
            await refunds
                .SumAsync(x => (decimal?)x.Amount)
            ?? 0m;


        // =========================================================
        // 4. STATISTICS BY DAY
        // =========================================================

        var dailyRows =
            await orders
                .Where(x =>
                    x.PlacedAt >= firstDayOfDailyStatistics &&
                    x.PlacedAt < tomorrow)
                .GroupBy(x => x.PlacedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,

                    TotalOrders = g.Count(),

                    Revenue =
                        g.Where(x =>
                                x.Status ==
                                OrderStatuses.Delivered)
                         .Sum(x =>
                             (decimal?)x.Total)
                        ?? 0m
                })
                .ToListAsync();

        var dailyDictionary =
            dailyRows.ToDictionary(
                x => x.Date.Date,
                x => x);

        var byDay =
            Enumerable
                .Range(0, 7)
                .Select(offset =>
                {
                    var date =
                        firstDayOfDailyStatistics
                            .AddDays(offset);

                    if (dailyDictionary.TryGetValue(
                            date.Date,
                            out var row))
                    {
                        return new DailyStatisticsDTO
                        {
                            Date = date.Date,
                            TotalOrders =
                                row.TotalOrders,
                            Revenue =
                                row.Revenue
                        };
                    }

                    return new DailyStatisticsDTO
                    {
                        Date = date.Date,
                        TotalOrders = 0,
                        Revenue = 0m
                    };
                })
                .ToList();


        // =========================================================
        // 5. STATISTICS BY MONTH
        // =========================================================

        var monthlyRows =
            await orders
                .Where(x =>
                    x.PlacedAt >= firstMonthOfStatistics &&
                    x.PlacedAt < firstDayOfNextMonth)
                .GroupBy(x => new
                {
                    x.PlacedAt.Year,
                    x.PlacedAt.Month
                })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,

                    TotalOrders =
                        g.Count(),

                    Revenue =
                        g.Where(x =>
                                x.Status ==
                                OrderStatuses.Delivered)
                         .Sum(x =>
                             (decimal?)x.Total)
                        ?? 0m
                })
                .ToListAsync();

        var monthlyDictionary =
            monthlyRows.ToDictionary(
                x => (x.Year, x.Month),
                x => x);

        var byMonth =
            Enumerable
                .Range(0, 12)
                .Select(offset =>
                {
                    var month =
                        firstMonthOfStatistics
                            .AddMonths(offset);

                    var key =
                        (month.Year, month.Month);

                    if (monthlyDictionary.TryGetValue(
                            key,
                            out var row))
                    {
                        return new MonthlyStatisticsDTO
                        {
                            Year = month.Year,
                            Month = month.Month,
                            TotalOrders =
                                row.TotalOrders,
                            Revenue =
                                row.Revenue
                        };
                    }

                    return new MonthlyStatisticsDTO
                    {
                        Year = month.Year,
                        Month = month.Month,
                        TotalOrders = 0,
                        Revenue = 0m
                    };
                })
                .ToList();


        // =========================================================
        // 6. STATISTICS BY SHOP
        // =========================================================

        var shopRows =
            await (
                from shop in shops

                join user in users
                    on shop.OwnerUserId equals user.Id

                select new
                {
                    ShopId = shop.Id,

                    ShopName = shop.Name,

                    SellerId = user.Id,

                    SellerName =
                        user.FullName ?? string.Empty,

                    SellerEmail =
                        user.Email
                }
            )
            .ToListAsync();

        var shopIds =
            shopRows
                .Select(x => x.ShopId)
                .ToList();

        var shopOrderRows =
            await orders
                .Where(x =>
                    shopIds.Contains(x.ShopId))
                .GroupBy(x => x.ShopId)
                .Select(g => new
                {
                    ShopId = g.Key,

                    TotalOrders =
                        g.Count(),

                    DeliveredOrders =
                        g.Count(x =>
                            x.Status ==
                            OrderStatuses.Delivered),

                    Revenue =
                        g.Where(x =>
                                x.Status ==
                                OrderStatuses.Delivered)
                         .Sum(x =>
                             (decimal?)x.Total)
                        ?? 0m
                })
                .ToListAsync();

        var shopOrderDictionary =
            shopOrderRows.ToDictionary(
                x => x.ShopId,
                x => x);

        var byShop =
            shopRows
                .Select(shop =>
                {
                    shopOrderDictionary.TryGetValue(
                        shop.ShopId,
                        out var orderData);

                    return new ShopStatisticsDTO
                    {
                        ShopId = shop.ShopId,

                        ShopName = shop.ShopName,

                        SellerId = shop.SellerId,

                        SellerName = shop.SellerName,

                        TotalOrders =
                            orderData?.TotalOrders ?? 0,

                        DeliveredOrders =
                            orderData?.DeliveredOrders ?? 0,

                        Revenue =
                            orderData?.Revenue ?? 0m
                    };
                })
                .OrderByDescending(x => x.Revenue)
                .ToList();


        // =========================================================
        // 7. STATISTICS BY SELLER
        // =========================================================

        var sellerRows =
            await (
                from user in users

                join userRole in userRoles
                    on user.Id equals userRole.UserId

                join role in roles
                    on userRole.RoleId equals role.Id

                where role.Name == "seller"

                select new
                {
                    SellerId = user.Id,

                    SellerName =
                        user.FullName ?? string.Empty,

                    Email =
                        user.Email
                }
            )
            .Distinct()
            .ToListAsync();

        var sellerIds =
            sellerRows
                .Select(x => x.SellerId)
                .ToList();

        var sellerShopRows =
            await shops
                .Where(x =>
                    sellerIds.Contains(
                        x.OwnerUserId))
                .GroupBy(x => x.OwnerUserId)
                .Select(g => new
                {
                    SellerId = g.Key,

                    TotalShops =
                        g.Count()
                })
                .ToListAsync();

        var sellerShopDictionary =
            sellerShopRows.ToDictionary(
                x => x.SellerId,
                x => x.TotalShops);

        var sellerOrderRows =
            await (
                from order in orders

                join shop in shops
                    on order.ShopId equals shop.Id

                where sellerIds.Contains(
                    shop.OwnerUserId)

                group order by shop.OwnerUserId
                into g

                select new
                {
                    SellerId = g.Key,

                    TotalOrders =
                        g.Count(),

                    DeliveredOrders =
                        g.Count(x =>
                            x.Status ==
                            OrderStatuses.Delivered),

                    Revenue =
                        g.Where(x =>
                                x.Status ==
                                OrderStatuses.Delivered)
                         .Sum(x =>
                             (decimal?)x.Total)
                        ?? 0m
                }
            )
            .ToListAsync();

        var sellerOrderDictionary =
            sellerOrderRows.ToDictionary(
                x => x.SellerId,
                x => x);

        var bySeller =
            sellerRows
                .Select(seller =>
                {
                    sellerOrderDictionary.TryGetValue(
                        seller.SellerId,
                        out var orderData);

                    sellerShopDictionary.TryGetValue(
                        seller.SellerId,
                        out var totalSellerShops);

                    return new SellerStatisticsDTO
                    {
                        SellerId =
                            seller.SellerId,

                        SellerName =
                            seller.SellerName,

                        Email =
                            seller.Email,

                        TotalShops =
                            totalSellerShops,

                        TotalOrders =
                            orderData?.TotalOrders ?? 0,

                        DeliveredOrders =
                            orderData?.DeliveredOrders ?? 0,

                        Revenue =
                            orderData?.Revenue ?? 0m
                    };
                })
                .OrderByDescending(x => x.Revenue)
                .ToList();


        // =========================================================
        // 8. STATISTICS BY ORDER STATUS
        // =========================================================

        var statusRows =
            await orders
                .GroupBy(x => x.Status)
                .Select(g => new OrderStatusStatisticsDTO
                {
                    Status =
                        g.Key,

                    TotalOrders =
                        g.Count(),

                    TotalAmount =
                        g.Sum(x => x.Total)
                })
                .OrderBy(x => x.Status)
                .ToListAsync();


        // =========================================================
        // 9. RETURN DASHBOARD
        // =========================================================

        return new AdminDashboardResponseDTO
        {
            Overview =
                new AdminOverviewDTO
                {
                    TotalRevenue =
                        totalRevenue,

                    TotalOrders =
                        totalOrders,

                    TodayOrders =
                        todayOrders,

                    ThisMonthOrders =
                        thisMonthOrders,

                    TotalUsers =
                        totalUsers,

                    TotalSellers =
                        totalSellers,

                    TotalShops =
                        totalShops,

                    TotalProducts =
                        totalProducts,

                    TotalRefunds =
                        totalRefunds,

                    TotalRefundAmount =
                        totalRefundAmount
                },

            ByDay = byDay,

            ByMonth = byMonth,

            ByShop = byShop,

            BySeller = bySeller,

            ByOrderStatus = statusRows
        };
    }
}