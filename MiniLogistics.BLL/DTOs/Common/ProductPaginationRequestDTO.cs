using MiniLogistics.BLL.DTOs.Common;

namespace MiniLogistics.BLL.DTOs.Product;

public class ProductPaginationRequestDTO : PaginationRequestDTO
{
    public string? Search { get; set; }

    public long? CategoryId { get; set; }

    public long? ShopId { get; set; }

    public string? Status { get; set; }
}