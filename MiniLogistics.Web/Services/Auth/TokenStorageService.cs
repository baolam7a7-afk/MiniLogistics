using Microsoft.JSInterop;

namespace MiniLogistics.Web.Services.Auth;

public class TokenStorageService
{
    private readonly IJSRuntime _jsRuntime;

    public TokenStorageService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    // =========================================================
    // SAVE TOKENS
    // =========================================================

    public async Task SetTokensAsync(
        string accessToken,
        string refreshToken)
    {
        await _jsRuntime.InvokeVoidAsync(
            "localStorage.setItem",
            "accessToken",
            accessToken);

        await _jsRuntime.InvokeVoidAsync(
            "localStorage.setItem",
            "refreshToken",
            refreshToken);
    }

    // =========================================================
    // GET ACCESS TOKEN
    // =========================================================

    public async Task<string?> GetAccessTokenAsync()
    {
        return await _jsRuntime.InvokeAsync<string?>(
            "localStorage.getItem",
            "accessToken");
    }

    // =========================================================
    // GET REFRESH TOKEN
    // =========================================================

    public async Task<string?> GetRefreshTokenAsync()
    {
        return await _jsRuntime.InvokeAsync<string?>(
            "localStorage.getItem",
            "refreshToken");
    }

    // =========================================================
    // CLEAR
    // =========================================================

    public async Task ClearAsync()
    {
        await _jsRuntime.InvokeVoidAsync(
            "localStorage.removeItem",
            "accessToken");

        await _jsRuntime.InvokeVoidAsync(
            "localStorage.removeItem",
            "refreshToken");
    }
}