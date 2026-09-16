namespace MiniLogistics.BLL.DTOs.Auth;

public class RegisterRequestDTO
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? FullName { get; set; }
}