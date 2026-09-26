using MiniLogistics.BLL.DTOs.Common;

namespace MiniLogistics.BLL.DTOs.Shipment;

public class ShipmentPaginationRequestDTO : PaginationRequestDTO
{
    public string? Search { get; set; }

    public string? Status { get; set; }

    public long? OrderId { get; set; }

    public long? ShipperUserId { get; set; }
}