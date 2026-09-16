using System;
using System.Collections.Generic;

namespace MiniLogistics.DAL.Models;

public class DisputeMessage
{
    public long Id { get; set; }
    public long DisputeId { get; set; }
    public long SenderUserId { get; set; }
    public string Message { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public Dispute Dispute { get; set; } = null!;
    public User SenderUser { get; set; } = null!;
}
