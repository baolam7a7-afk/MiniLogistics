namespace MiniLogistics.BLL.DTOs.Shipment;

public class ShipmentResponseDTO
{
    public long Id { get; set; }

    public long OrderId { get; set; }

    public long? ShipperUserId { get; set; }

    public string? ShipperName { get; set; }

    public string? TrackingCode { get; set; }

    public string Status { get; set; } = null!;

    public decimal CodAmount { get; set; }

    public DateTime? AssignedAt { get; set; }

    public DateTime? PickedAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public List<ShipmentEventResponseDTO> Events { get; set; }
        = new();
}