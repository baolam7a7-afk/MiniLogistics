using System;
using System.Collections.Generic;

namespace MiniLogistics.DAL.Models;

public class PaymentTransaction
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public string? Provider { get; set; }
    public string Method { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Status { get; set; } = "pending";
    public string? ProviderTxnId { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public Order Order { get; set; } = null!;
}
