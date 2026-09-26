namespace MiniLogistics.BLL.DTOs.Auth;

public class GoogleLoginRequestDTO
{
    public string IdToken { get; set; } = string.Empty;

    public string Role { get; set; } = "customer";
}