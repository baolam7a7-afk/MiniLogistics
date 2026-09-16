using System.ComponentModel.DataAnnotations;

namespace MiniLogistics.BLL.DTOs.Auth;

public class LogoutRequestDTO
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}