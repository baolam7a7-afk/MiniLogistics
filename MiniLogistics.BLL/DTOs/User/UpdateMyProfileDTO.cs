using System.ComponentModel.DataAnnotations;

namespace MiniLogistics.BLL.DTOs.User;

public class UpdateMyProfileDTO
{
    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(500)]
    public string? AvatarUrl { get; set; }
}