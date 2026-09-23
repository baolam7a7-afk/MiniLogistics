using System.ComponentModel.DataAnnotations;

namespace MiniLogistics.BLL.DTOs.Shop;

public class UpdateShopDTO
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = null!;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [MaxLength(1000)]
    public string? LogoUrl { get; set; }
}