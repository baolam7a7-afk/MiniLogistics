using System;
using System.Collections.Generic;

namespace MiniLogistics.DAL.Models;

public class PayoutRequest
{
    public long Id { get; set; }
    public long ShopId { get; set; }
    public decimal Amount { get; set; }
    public string BankAccountName { get; set; } = null!;
    public string BankAccountNumber { get; set; } = null!;
    public string BankName { get; set; } = null!;
    public string Status { get; set; } = "requested";
    public DateTime RequestedAt { get; set; }
    public long? ProcessedByUserId { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public Shop Shop { get; set; } = null!;
    public User? ProcessedByUser { get; set; }
}
