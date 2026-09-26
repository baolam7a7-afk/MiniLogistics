using MiniLogistics.BLL.DTOs.Common;

namespace MiniLogistics.BLL.DTOs.Voucher;

public class VoucherPaginationRequestDTO : PaginationRequestDTO
{
    public string? Search { get; set; }

    public string? Status { get; set; }

    public string? Scope { get; set; }

    public long? ShopId { get; set; }
}