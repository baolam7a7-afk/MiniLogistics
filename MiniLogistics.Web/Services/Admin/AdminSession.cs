using Microsoft.AspNetCore.Components;
using MiniLogistics.Web.Models.Seller;
using MiniLogistics.Web.Models.User;
using MiniLogistics.Web.Services.Auth;
using MiniLogistics.Web.Services.Seller;

namespace MiniLogistics.Web.Services.Admin;

public class AdminSession
{
    private readonly SellerApi _api;
    private readonly NavigationManager _navigation;
    private readonly TokenStorageService _tokens;
    private Task? _loading;
    private int _tokenVersion = -1;

    public AdminSession(
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
    public int ChatUnread { get; private set; }
    public int SupportUnread { get; private set; }

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

    public void NoteIncomingChat()
    {
        if (Profile == null || Forbidden)
        {
            return;
        }

        ChatUnread = ChatUnread >= 99 ? 100 : ChatUnread + 1;
        UiChanged?.Invoke();
    }

    public async Task RefreshChatUnreadAsync()
    {
        if (Profile == null || Forbidden)
        {
            return;
        }

        var result = await _api.GetAsync<List<SellerConversation>>("api/chat/conversations");
        if (!result.Ok || result.Data == null)
        {
            return;
        }

        ChatUnread = result.Data.Sum(item => item.UnreadCount < 0 ? 0 : item.UnreadCount);
        UiChanged?.Invoke();
        await RefreshSupportUnreadAsync();
    }

    public void NoteIncomingSupport()
    {
        if (Profile == null || Forbidden)
        {
            return;
        }

        SupportUnread = SupportUnread >= 99 ? 100 : SupportUnread + 1;
        UiChanged?.Invoke();
    }

    public async Task RefreshSupportUnreadAsync()
    {
        if (Profile == null || Forbidden)
        {
            return;
        }

        var result = await _api.GetAsync<List<SellerConversation>>("api/chat/platform");
        if (!result.Ok || result.Data == null)
        {
            return;
        }

        SupportUnread = result.Data.Sum(item => item.UnreadCount < 0 ? 0 : item.UnreadCount);
        UiChanged?.Invoke();
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
                role.Equals("admin", StringComparison.OrdinalIgnoreCase));
            Ready = true;
            UiChanged?.Invoke();
            if (!Forbidden)
            {
                await RefreshChatUnreadAsync();
            }
        }
        catch (Exception ex)
        {
            GateError = ex.Message;
            Ready = true;
            UiChanged?.Invoke();
        }
    }
}
