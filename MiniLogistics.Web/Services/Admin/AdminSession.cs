using Microsoft.AspNetCore.Components;
using MiniLogistics.Web.Models.User;
using MiniLogistics.Web.Services.Seller;

namespace MiniLogistics.Web.Services.Admin;

public class AdminSession
{
    private readonly SellerApi _api;
    private readonly NavigationManager _navigation;
    private Task? _loading;

    public AdminSession(SellerApi api, NavigationManager navigation)
    {
        _api = api;
        _navigation = navigation;
    }

    public bool Ready { get; private set; }
    public bool Forbidden { get; private set; }
    public string? GateError { get; private set; }
    public UserProfile? Profile { get; private set; }
    public bool NavOpen { get; private set; }

    public event Action? UiChanged;

    public Task EnsureAsync() => _loading ??= LoadAsync();

    public void ToggleNav()
    {
        NavOpen = !NavOpen;
        UiChanged?.Invoke();
    }

    public void CloseNav()
    {
        if (!NavOpen)
        {
            return;
        }

        NavOpen = false;
        UiChanged?.Invoke();
    }

    private async Task LoadAsync()
    {
        var profile = await _api.GetAsync<UserProfile>("api/users/me");
        if (profile.StatusCode == 401)
        {
            _navigation.NavigateTo("/login");
            return;
        }

        if (!profile.Ok || profile.Data == null)
        {
            GateError = profile.Error ?? "Không tải được tài khoản.";
            Ready = true;
            UiChanged?.Invoke();
            return;
        }

        Profile = profile.Data;
        Forbidden = !Profile.Roles.Any(role =>
            role.Equals("admin", StringComparison.OrdinalIgnoreCase));
        Ready = true;
        UiChanged?.Invoke();
    }
}
