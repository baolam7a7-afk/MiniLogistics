using MiniLogistics.BLL.DTOs.Common;

namespace MiniLogistics.BLL.DTOs.Category;

public class CategoryPaginationRequestDTO : PaginationRequestDTO
{
    public string? Search { get; set; }

    public bool? IsActive { get; set; }

    public long? ParentId { get; set; }
}