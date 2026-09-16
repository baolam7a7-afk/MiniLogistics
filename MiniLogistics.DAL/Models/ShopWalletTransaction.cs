using System;
using System.Collections.Generic;

namespace MiniLogistics.DAL.Models;

public class ShopWalletTransaction
{
    public long Id { get; set; }
    public long WalletId { get; set; }
    public long? OrderId { get; set; }
    public string Type { get; set; } = null!;
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public ShopWallet Wallet { get; set; } = null!;
    public Order? Order { get; set; }
}
