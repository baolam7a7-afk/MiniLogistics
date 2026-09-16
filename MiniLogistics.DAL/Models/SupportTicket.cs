using System;
using System.Collections.Generic;

namespace MiniLogistics.DAL.Models;

public class SupportTicket
{
    public long Id { get; set; }
    public long CreatedByUserId { get; set; }
    public long? OrderId { get; set; }
    public string Subject { get; set; } = null!;
    public string Status { get; set; } = "open";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public User CreatedByUser { get; set; } = null!;
    public Order? Order { get; set; }
    public ICollection<SupportMessage> SupportMessages { get; set; } = new List<SupportMessage>();
}
