using System;
using System.Collections.Generic;

namespace MiniLogistics.DAL.Models;

public class ShopWallet
{
    public long Id { get; set; }
    public long ShopId { get; set; }
    public decimal Balance { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Shop Shop { get; set; } = null!;
    public ICollection<ShopWalletTransaction> Transactions { get; set; } = new List<ShopWalletTransaction>();
}
