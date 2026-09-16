using System;
using System.Collections.Generic;

namespace MiniLogistics.DAL.Models;

public class CartItem
{
    public long Id { get; set; }
    public long CartId { get; set; }
    public long VariantId { get; set; }
    public int Quantity { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Cart Cart { get; set; } = null!;
    public ProductVariant Variant { get; set; } = null!;
}
