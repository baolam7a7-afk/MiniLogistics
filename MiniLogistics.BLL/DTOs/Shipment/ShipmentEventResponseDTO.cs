namespace MiniLogistics.BLL.DTOs.Shipment;

public class ShipmentEventResponseDTO
{
    public long Id { get; set; }

    public string Status { get; set; } = null!;

    public string? Location { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }
}