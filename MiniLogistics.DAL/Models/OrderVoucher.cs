using System;
using System.Collections.Generic;

namespace MiniLogistics.DAL.Models;

public class OrderVoucher
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public long VoucherId { get; set; }
    public string CodeSnapshot { get; set; } = null!;
    public decimal DiscountAmount { get; set; }
    public Order Order { get; set; } = null!;
    public Voucher Voucher { get; set; } = null!;
}
