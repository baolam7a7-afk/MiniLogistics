using MiniLogistics.BLL.DTOs.Common;

namespace MiniLogistics.BLL.DTOs.Shop;

public class ShopPaginationRequestDTO : PaginationRequestDTO
{
    public string? Search { get; set; }

    public string? Status { get; set; }
}