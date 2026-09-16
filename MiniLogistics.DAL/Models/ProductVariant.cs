using System;
using System.Collections.Generic;

namespace MiniLogistics.DAL.Models;

public class ProductVariant
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public string? Sku { get; set; }
    public string VariantName { get; set; } = null!;
    public string? AttributesJson { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Product Product { get; set; } = null!;
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
