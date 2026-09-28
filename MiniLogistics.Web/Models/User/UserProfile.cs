namespace MiniLogistics.Web.Models.User;

public class UserProfile
{
    public long Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? FullName { get; set; }

    public string? AvatarUrl { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public List<string> Roles { get; set; } = new();
}