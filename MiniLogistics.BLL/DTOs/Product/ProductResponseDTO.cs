namespace MiniLogistics.BLL.DTOs.Product;

public class ProductResponseDTO
{
    public long Id { get; set; }

    public long ShopId { get; set; }

    public string? ShopName { get; set; }

    public long CategoryId { get; set; }

    public string? CategoryName { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? InactiveReason { get; set; }

    public int? LowStockThreshold { get; set; }

    public string? ImageUrl { get; set; }

    public decimal? MinPrice { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}