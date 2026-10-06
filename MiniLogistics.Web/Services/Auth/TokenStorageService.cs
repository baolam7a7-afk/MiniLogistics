using Microsoft.JSInterop;

namespace MiniLogistics.Web.Services.Auth;

public class TokenStorageService
{
    private readonly IJSRuntime _jsRuntime;

    public TokenStorageService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public int Version { get; private set; }

    public async Task SetTokensAsync(
        string accessToken,
        string refreshToken)
    {
        Version++;
        await _jsRuntime.InvokeVoidAsync(
            "sessionStorage.setItem",
            "accessToken",
            accessToken);
        await _jsRuntime.InvokeVoidAsync(
            "sessionStorage.setItem",
            "refreshToken",
            refreshToken);
        await RemoveSharedCopiesAsync();
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        return await _jsRuntime.InvokeAsync<string?>(
            "sessionStorage.getItem",
            "accessToken");
    }

    public async Task<string?> GetRefreshTokenAsync()
    {
        return await _jsRuntime.InvokeAsync<string?>(
            "sessionStorage.getItem",
            "refreshToken");
    }

    public async Task ClearAsync()
    {
        Version++;
        await _jsRuntime.InvokeVoidAsync(
            "sessionStorage.removeItem",
            "accessToken");
        await _jsRuntime.InvokeVoidAsync(
            "sessionStorage.removeItem",
            "refreshToken");
        await RemoveSharedCopiesAsync();
    }

    private async Task RemoveSharedCopiesAsync()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "accessToken");
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "refreshToken");
    }
}
