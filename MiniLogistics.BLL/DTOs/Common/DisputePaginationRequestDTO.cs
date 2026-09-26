namespace MiniLogistics.BLL.DTOs.Common;

public class DisputePaginationRequestDTO : PaginationRequestDTO
{
    public string? Search { get; set; }

    public string? Status { get; set; }

    public long? OrderId { get; set; }
}