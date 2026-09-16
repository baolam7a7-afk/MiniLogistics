using System;
using System.Collections.Generic;

namespace MiniLogistics.DAL.Models;

public class User
{
    public long Id { get; set; }
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }
    public string PasswordHash { get; set; } = null!;
    public string? FullName { get; set; }
    public string? AvatarUrl { get; set; }
    public string Status { get; set; } = "active";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();
    public ICollection<Address> Addresses { get; set; } = new List<Address>();
    public ICollection<Shop> Shops { get; set; } = new List<Shop>();
    public ICollection<Order> CustomerOrders { get; set; } = new List<Order>();
    public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
    public ICollection<OrderStatusLog> OrderStatusLogs { get; set; } = new List<OrderStatusLog>();
    public ICollection<ReturnRequest> ReturnRequests { get; set; } = new List<ReturnRequest>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<ReviewReply> ReviewReplies { get; set; } = new List<ReviewReply>();
    public ICollection<SupportTicket> SupportTickets { get; set; } = new List<SupportTicket>();
    public ICollection<SupportMessage> SupportMessages { get; set; } = new List<SupportMessage>();
    public ICollection<Dispute> RaisedDisputes { get; set; } = new List<Dispute>();
    public ICollection<Dispute> HandledDisputes { get; set; } = new List<Dispute>();
    public ICollection<DisputeMessage> DisputeMessages { get; set; } = new List<DisputeMessage>();
    public ICollection<PayoutRequest> ProcessedPayoutRequests { get; set; } = new List<PayoutRequest>();
}
