using System;
using System.Collections.Generic;

namespace MiniLogistics.DAL.Models;

public class Shop
{
    public long Id { get; set; }
    public long OwnerUserId { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string Status { get; set; } = "pending";
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public User OwnerUser { get; set; } = null!;
    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Voucher> Vouchers { get; set; } = new List<Voucher>();
    public ShopWallet? ShopWallet { get; set; }
    public ICollection<PayoutRequest> PayoutRequests { get; set; } = new List<PayoutRequest>();
    public ICollection<ReviewReply> ReviewReplies { get; set; } = new List<ReviewReply>();
    public ICollection<ReportSnapshot> ReportSnapshots { get; set; } = new List<ReportSnapshot>();
}
