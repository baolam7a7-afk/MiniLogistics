using System;
using System.Collections.Generic;

namespace MiniLogistics.DAL.Models;

public class OrderItem
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public long ProductId { get; set; }
    public long VariantId { get; set; }
    public string ProductNameSnapshot { get; set; } = null!;
    public string VariantNameSnapshot { get; set; } = null!;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
    public Order Order { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public ProductVariant Variant { get; set; } = null!;
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
