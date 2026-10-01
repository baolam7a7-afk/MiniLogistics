namespace MiniLogistics.Web.Models.Auth;

public class GoogleLoginRequest
{
    public string IdToken { get; set; } = string.Empty;

    public string Role { get; set; } = "customer";
}