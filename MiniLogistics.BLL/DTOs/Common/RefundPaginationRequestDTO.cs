using MiniLogistics.BLL.DTOs.Common;

namespace MiniLogistics.BLL.DTOs.RefundTransaction;

public class RefundPaginationRequestDTO : PaginationRequestDTO
{
    public string? Search { get; set; }

    public string? Status { get; set; }

    public long? ReturnRequestId { get; set; }

    public long? OrderId { get; set; }
}