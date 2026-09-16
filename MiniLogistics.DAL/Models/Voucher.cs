using System;
using System.Collections.Generic;

namespace MiniLogistics.DAL.Models;

public class Voucher
{
    public long Id { get; set; }
    public string Scope { get; set; } = null!;
    public long? ShopId { get; set; }
    public string Code { get; set; } = null!;
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string DiscountType { get; set; } = null!;
    public decimal DiscountValue { get; set; }
    public decimal? MaxDiscount { get; set; }
    public decimal? MinOrderValue { get; set; }
    public int? UsageLimit { get; set; }
    public int UsedCount { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string Status { get; set; } = "active";
    public DateTime CreatedAt { get; set; }
    public Shop? Shop { get; set; }
    public ICollection<OrderVoucher> OrderVouchers { get; set; } = new List<OrderVoucher>();
}
