using MiniLogistics.BLL.DTOs.Common;

namespace MiniLogistics.BLL.DTOs.ReturnRequest;

public class ReturnRequestPaginationRequestDTO : PaginationRequestDTO
{
    public string? Search { get; set; }

    public string? Status { get; set; }
}