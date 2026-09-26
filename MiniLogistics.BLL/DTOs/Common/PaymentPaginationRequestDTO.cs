using MiniLogistics.BLL.DTOs.Common;

namespace MiniLogistics.BLL.DTOs.Payment;

public class PaymentPaginationRequestDTO : PaginationRequestDTO
{
    public string? Search { get; set; }

    public string? Status { get; set; }

    public long? OrderId { get; set; }
}