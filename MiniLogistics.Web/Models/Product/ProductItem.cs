namespace MiniLogistics.Web.Models.Product;

public class ProductItem
{
    public long Id { get; set; }

    public long ShopId { get; set; }

    public long CategoryId { get; set; }

    public string? CategoryName { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public decimal? MinPrice { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
