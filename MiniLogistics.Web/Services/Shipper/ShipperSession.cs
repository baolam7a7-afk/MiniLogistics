using Microsoft.AspNetCore.Components;
using MiniLogistics.Web.Models.User;
using MiniLogistics.Web.Services.Auth;
using MiniLogistics.Web.Services.Seller;

namespace MiniLogistics.Web.Services.Shipper;

public class ShipperSession
{
    private readonly SellerApi _api;
    private readonly NavigationManager _navigation;
    private readonly TokenStorageService _tokens;
    private Task? _loading;
    private int _tokenVersion = -1;

    public ShipperSession(
        SellerApi api,
        NavigationManager navigation,
        TokenStorageService tokens)
    {
        _api = api;
        _navigation = navigation;
        _tokens = tokens;
    }

    public bool Ready { get; private set; }
    public bool Forbidden { get; private set; }
    public string? GateError { get; private set; }
    public UserProfile? Profile { get; private set; }

    public void ApplyAvatar(string? avatarUrl)
    {
        if (Profile != null)
        {
            Profile.AvatarUrl = avatarUrl;
        }

        UiChanged?.Invoke();
    }
    public bool NavOpen { get; private set; }

    public event Action? UiChanged;

    public Task EnsureAsync()
    {
        if (_loading is { IsCompleted: false } && _tokenVersion == _tokens.Version)
        {
            return _loading;
        }

        if (_loading is null || !Ready || _tokenVersion != _tokens.Version)
        {
            _tokenVersion = _tokens.Version;
            Ready = false;
            Profile = null;
            Forbidden = false;
            GateError = null;
            _loading = LoadAsync();
        }

        return _loading;
    }

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
        var version = _tokenVersion;
        try
        {
            var profile = await _api.GetAsync<UserProfile>("api/users/me");
            if (version != _tokenVersion)
            {
                return;
            }

            if (profile.StatusCode == 401)
            {
                GateError = profile.Error ?? "Phiên đăng nhập đã hết. Vui lòng đăng nhập lại.";
                Ready = true;
                UiChanged?.Invoke();
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
            var roles = Profile.Roles ?? new List<string>();
            Forbidden = !roles.Any(role =>
                role.Equals("shipper", StringComparison.OrdinalIgnoreCase));
            Ready = true;
            UiChanged?.Invoke();
        }
        catch (Exception ex)
        {
            GateError = ex.Message;
            Ready = true;
            UiChanged?.Invoke();
        }
    }
}
