using System.ComponentModel.DataAnnotations;

namespace MiniLogistics.BLL.DTOs.Product;

public class UpdateProductDTO
{
    [Required]
    public long ShopId { get; set; }

    [Required]
    public long CategoryId { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public string Status { get; set; } = "active";
}

public class AdminProductEditDTO
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Status { get; set; } = "active";

    public List<AdminVariantEditDTO> Variants { get; set; } = new();
}

public class AdminVariantEditDTO
{
    public long Id { get; set; }

    public string VariantName { get; set; } = string.Empty;

    public decimal Price { get; set; }
}