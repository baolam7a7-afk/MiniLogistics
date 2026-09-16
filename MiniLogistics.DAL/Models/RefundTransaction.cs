using System;
using System.Collections.Generic;

namespace MiniLogistics.DAL.Models;

public class RefundTransaction
{
    public long Id { get; set; }
    public long ReturnRequestId { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = null!;
    public string Status { get; set; } = "pending";
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public ReturnRequest ReturnRequest { get; set; } = null!;
}
