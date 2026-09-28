
using MiniLogistics.Web.Models.Auth;

namespace MiniLogistics.Web.Services.Auth;

public class AuthStateService
{
    public bool IsAuthenticated { get; private set; }
    public UserInfo? CurrentUser { get; private set; }

    public event Action? OnChange;

    public void SetUser(AuthResponse response)
    {
        CurrentUser = new UserInfo
        {
            UserId = response.UserId,
            Email = response.Email,
            FullName = response.FullName,
            Roles = response.Roles
        };

        IsAuthenticated = true;
        NotifyStateChanged();
    }


    public void UpdateUserProfile(string? fullName, string? avatarUrl)
    {
        if (CurrentUser == null)
        {
            CurrentUser = new UserInfo
            {
                FullName = fullName ?? string.Empty,
                AvatarUrl = avatarUrl
            };

            IsAuthenticated = true;
        }
        else
        {
            CurrentUser.FullName = fullName ?? string.Empty;
            CurrentUser.AvatarUrl = avatarUrl;
        }

        NotifyStateChanged();
    }

    public void Logout()
    {
        CurrentUser = null;
        IsAuthenticated = false;
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        OnChange?.Invoke();
    }
}