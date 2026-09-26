using MiniLogistics.BLL.DTOs.Common;

namespace MiniLogistics.BLL.DTOs.ProductVariant;

public class ProductVariantPaginationRequestDTO : PaginationRequestDTO
{
    public string? Search { get; set; }

    public long? ProductId { get; set; }

    public bool? IsActive { get; set; }
}