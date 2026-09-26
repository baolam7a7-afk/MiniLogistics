using MiniLogistics.Web.Models.Auth;

namespace MiniLogistics.Web.Services.Auth;

public class AuthStateService
{
    public bool IsAuthenticated { get; private set; }

    public UserInfo? CurrentUser { get; private set; }

    // =========================================================
    // LOGIN
    // =========================================================

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
    }

    // =========================================================
    // LOGOUT
    // =========================================================

    public void Logout()
    {
        CurrentUser = null;

        IsAuthenticated = false;
    }
}