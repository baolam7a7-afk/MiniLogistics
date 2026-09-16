using System;
using System.Collections.Generic;

namespace MiniLogistics.DAL.Models;

public class UserSession
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string RefreshTokenHash { get; set; } = null!;
    public string? UserAgent { get; set; }
    public string? Ip { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public User User { get; set; } = null!;
}
