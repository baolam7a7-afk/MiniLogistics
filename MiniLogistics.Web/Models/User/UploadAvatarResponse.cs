
namespace MiniLogistics.Web.Models.User;

public class UploadAvatarResponse
{
    public string AvatarUrl { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}

public class AvatarUploadResult
{
    public string? AvatarUrl { get; set; }
    public string? Error { get; set; }
}