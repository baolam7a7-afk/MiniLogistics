using MiniLogistics.BLL.DTOs.Common;

namespace MiniLogistics.BLL.DTOs.PayoutRequest;

public class PayoutRequestPaginationRequestDTO : PaginationRequestDTO
{
    public string? Search { get; set; }

    public string? Status { get; set; }

    public long? ShopId { get; set; }
}