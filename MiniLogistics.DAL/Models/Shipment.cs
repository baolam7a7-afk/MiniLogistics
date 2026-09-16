using System;
using System.Collections.Generic;

namespace MiniLogistics.DAL.Models;

public class Shipment
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public long? ShipperUserId { get; set; }
    public string? TrackingCode { get; set; }
    public string Status { get; set; } = "created";
    public decimal CodAmount { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? PickedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Order Order { get; set; } = null!;
    public User? ShipperUser { get; set; }
    public ICollection<ShipmentEvent> ShipmentEvents { get; set; } = new List<ShipmentEvent>();
}
