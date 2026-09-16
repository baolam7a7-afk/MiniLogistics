using System;
using System.Collections.Generic;

namespace MiniLogistics.DAL.Models;

public class Product
{
    public long Id { get; set; }
    public long ShopId { get; set; }
    public long CategoryId { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Description { get; set; }
    public string Status { get; set; } = "draft";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Shop Shop { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
    public ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
