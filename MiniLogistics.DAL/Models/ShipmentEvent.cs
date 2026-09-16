using System;
using System.Collections.Generic;

namespace MiniLogistics.DAL.Models;

public class ShipmentEvent
{
    public long Id { get; set; }
    public long ShipmentId { get; set; }
    public string Status { get; set; } = null!;
    public string? Location { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public Shipment Shipment { get; set; } = null!;
}
