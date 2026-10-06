using System.Net.Http.Json;

namespace MiniLogistics.Web.Services.Voucher;

public class VoucherOffer
{
    public string Code { get; set; } = "";
    public string Scope { get; set; } = "";
    public long? ShopId { get; set; }
    public string DiscountType { get; set; } = "";
    public decimal DiscountValue { get; set; }
    public decimal? MaxDiscount { get; set; }
    public decimal? MinOrderValue { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string Status { get; set; } = "";
    public List<long> ProductIds { get; set; } = new();
    public List<string> ProductNames { get; set; } = new();

    public bool AppliesTo(long productId, long shopId)
    {
        if (string.Equals(Scope, "shop", StringComparison.OrdinalIgnoreCase) &&
            ShopId != shopId)
        {
            return false;
        }

        return ProductIds.Count == 0 || ProductIds.Contains(productId);
    }

    public string TargetText =>
        ProductNames.Count > 0
            ? string.Join(", ", ProductNames)
            : ProductIds.Count > 0
                ? string.Join(", ", ProductIds.Select(id => $"sản phẩm #{id}"))
                : string.Equals(Scope, "shop", StringComparison.OrdinalIgnoreCase)
                    ? "tất cả sản phẩm của shop"
                    : "tất cả sản phẩm";

    public string DiscountText =>
        string.Equals(DiscountType, "percent", StringComparison.OrdinalIgnoreCase)
            ? $"Giảm {DiscountValue:0.#}%"
            : $"Giảm {DiscountValue:N0} ₫";

    public decimal Off(decimal basis)
    {
        if (basis <= 0)
        {
            return 0;
        }

        if (MinOrderValue is decimal minimum && basis < minimum)
        {
            return 0;
        }

        var amount = string.Equals(DiscountType, "percent", StringComparison.OrdinalIgnoreCase)
            ? basis * DiscountValue / 100m
            : DiscountValue;
        if (MaxDiscount is decimal cap && amount > cap)
        {
            amount = cap;
        }

        if (amount > basis)
        {
            amount = basis;
        }

        return amount < 0
            ? 0
            : Math.Round(amount, 0, MidpointRounding.AwayFromZero);
    }
}

public class ProductPriceQuote
{
    public string Code { get; set; } = "";
    public decimal Original { get; set; }
    public decimal Off { get; set; }
    public decimal Sale => Original - Off;
    public string Note { get; set; } = "";
}

public class VoucherOfferService
{
    private readonly HttpClient _http;
    private Task<IReadOnlyList<VoucherOffer>>? _loading;

    public VoucherOfferService(HttpClient http)
    {
        _http = http;
    }

    public Task<IReadOnlyList<VoucherOffer>> GetActiveAsync() =>
        _loading ??= LoadAsync();

    public async Task<ProductPriceQuote?> QuoteAsync(long productId, long shopId, decimal price)
    {
        var offers = await GetActiveAsync();
        return Quote(offers, productId, shopId, price);
    }

    public static ProductPriceQuote? Quote(
        IReadOnlyList<VoucherOffer> offers,
        long productId,
        long shopId,
        decimal price)
    {
        VoucherOffer? best = null;
        var bestOff = 0m;
        foreach (var offer in offers)
        {
            if (offer.ProductIds.Count == 0 || !offer.AppliesTo(productId, shopId))
            {
                continue;
            }

            var off = offer.Off(price);
            if (off > bestOff)
            {
                best = offer;
                bestOff = off;
            }
        }

        if (best == null || bestOff <= 0)
        {
            return null;
        }

        var target = best.ProductIds.Count == 0 ? best.TargetText : "sản phẩm này";
        return new ProductPriceQuote
        {
            Code = best.Code,
            Original = price,
            Off = bestOff,
            Note = $"{best.Code} trừ {bestOff:N0} ₫ cho {target}"
        };
    }

    private async Task<IReadOnlyList<VoucherOffer>> LoadAsync()
    {
        try
        {
            var page = await _http.GetFromJsonAsync<VoucherPage>(
                "api/vouchers?Status=active&Page=1&PageSize=100");
            return (page?.Items ?? new())
                .Where(IsOpen)
                .ToList();
        }
        catch
        {
            return Array.Empty<VoucherOffer>();
        }
    }

    private static bool IsOpen(VoucherOffer offer)
    {
        if (!string.Equals(offer.Status, "active", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var utc = DateTime.UtcNow;
        var local = DateTime.Now;
        var started = offer.StartAt <= utc || offer.StartAt <= local;
        var ended = offer.EndAt < utc && offer.EndAt < local;
        return started && !ended;
    }

    private sealed class VoucherPage
    {
        public List<VoucherOffer> Items { get; set; } = new();
    }
}
