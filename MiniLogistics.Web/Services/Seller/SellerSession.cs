using Microsoft.AspNetCore.Components;
using MiniLogistics.Web.Models.Seller;
using MiniLogistics.Web.Models.User;

namespace MiniLogistics.Web.Services.Seller;

public class SellerSession
{
    private readonly SellerApi _api;
    private readonly NavigationManager _navigation;
    private Task? _loading;

    public SellerSession(SellerApi api, NavigationManager navigation)
    {
        _api = api;
        _navigation = navigation;
    }

    public bool Ready { get; private set; }
    public bool Forbidden { get; private set; }
    public string? GateError { get; private set; }
    public UserProfile? Profile { get; private set; }
    public List<ShopRecord> Shops { get; private set; } = new();
    public ShopRecord? Shop { get; private set; }
    public bool NavOpen { get; private set; }

    public event Action? ShopChanged;
    public event Action? UiChanged;

    public Task EnsureAsync() => _loading ??= LoadAsync();

    public void SelectShop(long shopId)
    {
        var next = Shops.FirstOrDefault(shop => shop.Id == shopId);
        if (next == null || Shop?.Id == next.Id)
        {
            return;
        }

        Shop = next;
        ShopChanged?.Invoke();
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
            role.Equals("seller", StringComparison.OrdinalIgnoreCase));

        if (!Forbidden)
        {
            var shops = await _api.GetAsync<List<ShopRecord>>("api/shops/my");
            if (shops.StatusCode == 401)
            {
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
    }
}
