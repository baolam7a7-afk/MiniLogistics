using System;
using System.Collections.Generic;

namespace MiniLogistics.DAL.Models;

public class OrderStatusLog
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = null!;
    public string? Message { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public Order Order { get; set; } = null!;
    public User? CreatedByUser { get; set; }
}
