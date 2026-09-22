namespace MiniLogistics.BLL.DTOs.Shop;

public class ShopResponseDto
{
    public long Id { get; set; }

    public long OwnerUserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? LogoUrl { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime? ApprovedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}