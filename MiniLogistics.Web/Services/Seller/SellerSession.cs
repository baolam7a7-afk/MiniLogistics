using Microsoft.AspNetCore.Components;
using MiniLogistics.Web.Models.Seller;
using MiniLogistics.Web.Models.User;
using MiniLogistics.Web.Services.Auth;

namespace MiniLogistics.Web.Services.Seller;

public class SellerSession
{
    private readonly SellerApi _api;
    private readonly NavigationManager _navigation;
    private readonly TokenStorageService _tokens;
    private Task? _loading;
    private int _tokenVersion = -1;

    public SellerSession(
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
    public List<ShopRecord> Shops { get; private set; } = new();
    public ShopRecord? Shop { get; private set; }
    public bool NavOpen { get; private set; }
    public int ChatUnread { get; private set; }

    public event Action? ShopChanged;
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
            Shops = new();
            Shop = null;
            _loading = LoadAsync();
        }

        return _loading;
    }

    public void SelectShop(long shopId)
    {
        var next = Shops.FirstOrDefault(shop => shop.Id == shopId);
        if (next == null || Shop?.Id == next.Id)
        {
            return;
        }

        Shop = next;
        ShopChanged?.Invoke();
        _ = RefreshChatUnreadAsync();
    }

    public void RememberShop(ShopRecord shop)
    {
        var index = Shops.FindIndex(item => item.Id == shop.Id);
        if (index >= 0)
        {
            Shops[index] = shop;
        }
        else
        {
            Shops.Insert(0, shop);
        }

        Shop = shop;
        ShopChanged?.Invoke();
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

        var shopId = Shop?.Id;
        ChatUnread = result.Data
            .Where(item => shopId == null || item.ShopId == shopId)
            .Sum(item => item.UnreadCount < 0 ? 0 : item.UnreadCount);
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
                role.Equals("seller", StringComparison.OrdinalIgnoreCase));

            if (!Forbidden)
            {
                var shops = await _api.GetAsync<List<ShopRecord>>("api/shops/my");
                if (shops.StatusCode == 401)
                {
                    GateError = "Phiên đăng nhập đã hết. Vui lòng đăng nhập lại.";
                    Ready = true;
                    UiChanged?.Invoke();
                    _navigation.NavigateTo("/login");
                    return;
                }

                Shops = shops.Data ?? new();
                Shop = Shops.FirstOrDefault();
                if (!shops.Ok && shops.StatusCode != 404)
                {
                    GateError = shops.Error;
                }
            }

            Ready = true;
            UiChanged?.Invoke();
            await RefreshChatUnreadAsync();
        }
        catch (Exception ex)
        {
            GateError = ex.Message;
            Ready = true;
            UiChanged?.Invoke();
        }
    }
}
