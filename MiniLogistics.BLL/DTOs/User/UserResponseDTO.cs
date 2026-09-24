namespace MiniLogistics.BLL.DTOs.User;

public class UserResponseDTO
{
    public long Id { get; set; }

    public string Email { get; set; } = null!;

    public string? Phone { get; set; }

    public string? FullName { get; set; }

    public string? AvatarUrl { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public List<string> Roles { get; set; } = new();
}