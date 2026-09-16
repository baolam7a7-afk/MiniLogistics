using System;
using System.Collections.Generic;

namespace MiniLogistics.DAL.Models;

public class Review
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public long OrderItemId { get; set; }
    public long ProductId { get; set; }
    public long CustomerId { get; set; }
    public int Rating { get; set; }
    public string? Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Order Order { get; set; } = null!;
    public OrderItem OrderItem { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public User Customer { get; set; } = null!;
    public ReviewReply? ReviewReply { get; set; }
}
