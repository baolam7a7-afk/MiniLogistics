using MiniLogistics.BLL.DTOs.Common;

namespace MiniLogistics.BLL.DTOs.Review;

public class ReviewPaginationRequestDTO : PaginationRequestDTO
{
    public string? Search { get; set; }

    public int? Rating { get; set; }
}