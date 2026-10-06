using Microsoft.EntityFrameworkCore;
using MiniLogistics.DAL.UnitOfWork;
using OrderEntity = MiniLogistics.DAL.Models.Order;

namespace MiniLogistics.BLL.Services.Finance;

public class FinanceLine
{
    public long OrderId { get; set; }
    public string OrderCode { get; set; } = "";
    public long ShopId { get; set; }
    public string ShopName { get; set; } = "";
    public string Products { get; set; } = "";
    public decimal ProductSales { get; set; }
    public decimal PlatformFee { get; set; }
    public decimal SellerShare { get; set; }
    public DateTime PlacedAt { get; set; }
}

public class FinanceReport
{
    public decimal ProductSales { get; set; }
    public decimal PlatformFee { get; set; }
    public decimal SellerShare { get; set; }
    public List<FinanceLine> Lines { get; set; } = new();
}

public interface IFinanceService
{
    Task<FinanceReport> GetPlatformAsync(long? shopId = null);
    Task<FinanceReport> GetSellerShopAsync(long ownerUserId, long shopId);
}

public class FinanceService : IFinanceService
{
    public const decimal PlatformRate = 0.05m;
    private readonly IUnitOfWork _uow;

    public FinanceService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<FinanceReport> GetPlatformAsync(long? shopId = null) =>
        BuildAsync(shopId, null);

    public async Task<FinanceReport> GetSellerShopAsync(long ownerUserId, long shopId)
    {
        var owns = await _uow.Shops.Query().AnyAsync(shop =>
            shop.Id == shopId && shop.OwnerUserId == ownerUserId);
        if (!owns)
        {
            return new FinanceReport();
        }

        return await BuildAsync(shopId, ownerUserId);
    }

    private async Task<FinanceReport> BuildAsync(long? shopId, long? ownerUserId)
    {
        var query = _uow.Orders.Query()
            .AsNoTracking()
            .Include(order => order.Shop)
            .Include(order => order.OrderItems)
            .Where(order => order.Status == "delivered");

        if (shopId.HasValue)
        {
            query = query.Where(order => order.ShopId == shopId.Value);
        }

        if (ownerUserId.HasValue)
        {
            query = query.Where(order => order.Shop.OwnerUserId == ownerUserId.Value);
        }

        var orders = await query
            .OrderByDescending(order => order.PlacedAt)
            .Take(200)
            .ToListAsync();

        var lines = orders.Select(Map).ToList();
        return new FinanceReport
        {
            ProductSales = lines.Sum(line => line.ProductSales),
            PlatformFee = lines.Sum(line => line.PlatformFee),
            SellerShare = lines.Sum(line => line.SellerShare),
            Lines = lines
        };
    }

    private static FinanceLine Map(OrderEntity order)
    {
        var sales = order.Subtotal > 0
            ? Math.Max(0, order.Subtotal - order.DiscountTotal)
            : Math.Max(0, order.Total);
        var fee = Math.Round(sales * PlatformRate, 0, MidpointRounding.AwayFromZero);
        return new FinanceLine
        {
            OrderId = order.Id,
            OrderCode = order.OrderCode,
            ShopId = order.ShopId,
            ShopName = order.Shop?.Name ?? $"Shop #{order.ShopId}",
            Products = string.Join(", ", order.OrderItems.Select(item => item.ProductNameSnapshot)),
            ProductSales = sales,
            PlatformFee = fee,
            SellerShare = sales - fee,
            PlacedAt = order.PlacedAt
        };
    }
}
