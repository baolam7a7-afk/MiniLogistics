using System;
using System.Collections.Generic;

namespace MiniLogistics.DAL.Models;

public class ReturnRequest
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public long CustomerId { get; set; }
    public string Reason { get; set; } = null!;
    public string? Description { get; set; }
    public string Status { get; set; } = "requested";
    public DateTime RequestedAt { get; set; }
    public long? HandledByUserId { get; set; }
    public DateTime? HandledAt { get; set; }
    public Order Order { get; set; } = null!;
    public User Customer { get; set; } = null!;
    public User? HandledByUser { get; set; }
    public ICollection<RefundTransaction> RefundTransactions { get; set; } = new List<RefundTransaction>();
}
