using System;
using System.Collections.Generic;

namespace MiniLogistics.DAL.Models;

public class Dispute
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public long RaisedByUserId { get; set; }
    public string Reason { get; set; } = null!;
    public string Status { get; set; } = "open";
    public DateTime CreatedAt { get; set; }
    public long? HandledByUserId { get; set; }
    public DateTime? HandledAt { get; set; }
    public Order Order { get; set; } = null!;
    public User RaisedByUser { get; set; } = null!;
    public User? HandledByUser { get; set; }
    public ICollection<DisputeMessage> DisputeMessages { get; set; } = new List<DisputeMessage>();
}
