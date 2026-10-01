namespace MiniLogistics.Web.Models.Category;

public class CategoryListResponse
{
    public List<CategoryItem> Items { get; set; } = new();

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalItems { get; set; }

    public int TotalPages { get; set; }
}
