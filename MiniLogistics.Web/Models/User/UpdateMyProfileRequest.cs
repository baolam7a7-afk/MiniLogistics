
namespace MiniLogistics.Web.Models.User;

public class UpdateMyProfileRequest
{
    public string FullName { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? AvatarUrl { get; set; }
}