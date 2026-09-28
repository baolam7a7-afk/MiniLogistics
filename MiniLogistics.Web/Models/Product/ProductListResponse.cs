namespace MiniLogistics.Web.Models.Product;

public class ProductListResponse
{
    public List<ProductItem> Items { get; set; } = new();

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalItems { get; set; }

    public int TotalPages { get; set; }
}