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

    // ==========================================
    // AUTHENTICATION
    // ==========================================

    public string? AuthProvider { get; set; }

    public string? GoogleId { get; set; }

    // ==========================================
    // USER RELATIONSHIPS
    // ==========================================

    public ICollection<UserRole> UserRoles { get; set; }
        = new List<UserRole>();

    public ICollection<UserSession> UserSessions { get; set; }
        = new List<UserSession>();

    // ==========================================
    // ADDRESS
    // ==========================================

    public ICollection<Address> Addresses { get; set; }
        = new List<Address>();

    // ==========================================
    // SHOP
    // ==========================================

    public ICollection<Shop> Shops { get; set; }
        = new List<Shop>();

    // ==========================================
    // ORDER
    // ==========================================

    public ICollection<Order> CustomerOrders { get; set; }
        = new List<Order>();

    public ICollection<OrderStatusLog> OrderStatusLogs { get; set; }
        = new List<OrderStatusLog>();

    // ==========================================
    // SHIPMENT
    // ==========================================

    public ICollection<Shipment> Shipments { get; set; }
        = new List<Shipment>();

    // ==========================================
    // RETURN REQUEST
    // ==========================================

    public ICollection<ReturnRequest> ReturnRequests { get; set; }
        = new List<ReturnRequest>();

    // ==========================================
    // REVIEW
    // ==========================================

    public ICollection<Review> Reviews { get; set; }
        = new List<Review>();

    public ICollection<ReviewReply> ReviewReplies { get; set; }
        = new List<ReviewReply>();

    // ==========================================
    // SUPPORT
    // ==========================================

    public ICollection<SupportTicket> SupportTickets { get; set; }
        = new List<SupportTicket>();

    public ICollection<SupportMessage> SupportMessages { get; set; }
        = new List<SupportMessage>();

    // ==========================================
    // DISPUTE
    // ==========================================

    public ICollection<Dispute> RaisedDisputes { get; set; }
        = new List<Dispute>();

    public ICollection<Dispute> HandledDisputes { get; set; }
        = new List<Dispute>();

    public ICollection<DisputeMessage> DisputeMessages { get; set; }
        = new List<DisputeMessage>();

    // ==========================================
    // PAYOUT
    // ==========================================

    public ICollection<PayoutRequest> ProcessedPayoutRequests { get; set; }
        = new List<PayoutRequest>();
}