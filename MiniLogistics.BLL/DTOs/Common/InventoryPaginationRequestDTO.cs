using MiniLogistics.BLL.DTOs.Common;

namespace MiniLogistics.BLL.DTOs.Inventory;

public class InventoryPaginationRequestDTO : PaginationRequestDTO
{
    public string? Search { get; set; }

    public long? ProductVariantId { get; set; }

    public bool? IsActive { get; set; }
}