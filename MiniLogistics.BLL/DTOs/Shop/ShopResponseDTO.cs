namespace MiniLogistics.BLL.DTOs.Shop;

public class ShopResponseDTO
{
    public long Id { get; set; }

    public long OwnerUserId { get; set; }

    public string Name { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string? Description { get; set; }

    public string? LogoUrl { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? ApprovedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}