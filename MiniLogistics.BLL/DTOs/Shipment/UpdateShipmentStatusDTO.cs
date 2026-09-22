namespace MiniLogistics.BLL.DTOs.Shipment;

public class UpdateShipmentStatusDTO
{
    public string Status { get; set; } = null!;

    public string? Location { get; set; }

    public string? Note { get; set; }
}