using MiniLogistics.BLL.DTOs.Common;

namespace MiniLogistics.BLL.DTOs.Order;

public class OrderPaginationRequestDTO : PaginationRequestDTO
{
    public string? Search { get; set; }

    public string? Status { get; set; }
}