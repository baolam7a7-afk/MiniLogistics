namespace MiniLogistics.BLL.DTOs.Auth;

public class RegisterRequestDTO
{
    public string? Email { get; set; }

    public string? Password { get; set; }

    public string? Phone { get; set; }

    public string? FullName { get; set; }

    public string Role { get; set; } = "customer";
}