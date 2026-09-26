namespace MiniLogistics.BLL.DTOs.Auth;

public class ResetPasswordRequestDTO
{
    public string Token { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;
}