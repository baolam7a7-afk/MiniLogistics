namespace MiniLogistics.BLL.DTOs.Common;

public class UserPaginationRequestDTO : PaginationRequestDTO
{
    public string? Search { get; set; }

    public string? Role { get; set; }

    public string? Status { get; set; }
}