using System;
using System.Collections.Generic;

namespace MiniLogistics.DAL.Models;

public class Order
{
    public long Id { get; set; }
    public string OrderCode { get; set; } = null!;
    public long CustomerId { get; set; }
    public long ShopId { get; set; }
    public long ShippingAddressId { get; set; }
    public string Status { get; set; } = "pending";
    public string Currency { get; set; } = "VND";
    public decimal Subtotal { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal Total { get; set; }
    public string PaymentMethod { get; set; } = null!;
    public string? Note { get; set; }
    public DateTime PlacedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public User Customer { get; set; } = null!;
    public Shop Shop { get; set; } = null!;
    public Address ShippingAddress { get; set; } = null!;
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<OrderStatusLog> OrderStatusLogs { get; set; } = new List<OrderStatusLog>();
    public ICollection<OrderVoucher> OrderVouchers { get; set; } = new List<OrderVoucher>();
    public ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
    public Shipment? Shipment { get; set; }
    public ICollection<ReturnRequest> ReturnRequests { get; set; } = new List<ReturnRequest>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<SupportTicket> SupportTickets { get; set; } = new List<SupportTicket>();
    public ICollection<Dispute> Disputes { get; set; } = new List<Dispute>();
    public ICollection<ShopWalletTransaction> ShopWalletTransactions { get; set; } = new List<ShopWalletTransaction>();
}
