namespace MiniLogistics.Web.Models.Auth;

public class UserInfo
{
    public long UserId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public List<string> Roles { get; set; } = new();

    public string Role =>
        Roles.FirstOrDefault() ?? string.Empty;
}