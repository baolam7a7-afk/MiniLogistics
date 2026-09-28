using System.Net.Http.Headers;
using System.Net.Http.Json;
using MiniLogistics.Web.Models.Cart;
using MiniLogistics.Web.Services.Auth;

namespace MiniLogistics.Web.Services.Cart;

public class CartService
{
    private readonly HttpClient _httpClient;
    private readonly TokenStorageService _tokenStorage;

    public CartService(
        HttpClient httpClient,
        TokenStorageService tokenStorage)
    {
        _httpClient = httpClient;
        _tokenStorage = tokenStorage;
    }

    // =========================================================
    // GET CART
    // =========================================================

    public async Task<CartResponse?> GetCartAsync()
    {
        var accessToken =
            await _tokenStorage.GetAccessTokenAsync();

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                "api/cart");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        var response =
            await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content
            .ReadFromJsonAsync<CartResponse>();
    }


    // =========================================================
    // ADD ITEM
    // =========================================================

    public async Task<bool> AddItemAsync(
        AddCartItemRequest request)
    {
        var accessToken =
            await _tokenStorage.GetAccessTokenAsync();

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return false;
        }

        using var httpRequest =
            new HttpRequestMessage(
                HttpMethod.Post,
                "api/cart/items");

        httpRequest.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        httpRequest.Content =
            JsonContent.Create(request);

        var response =
            await _httpClient.SendAsync(httpRequest);

        return response.IsSuccessStatusCode;
    }


    // =========================================================
    // UPDATE ITEM QUANTITY
    // =========================================================

    public async Task<bool> UpdateItemQuantityAsync(
        long cartItemId,
        int quantity)
    {
        var accessToken =
            await _tokenStorage.GetAccessTokenAsync();

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return false;
        }

        var requestBody = new
        {
            quantity
        };

        using var httpRequest =
            new HttpRequestMessage(
                HttpMethod.Put,
                $"api/cart/items/{cartItemId}");

        httpRequest.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        httpRequest.Content =
            JsonContent.Create(requestBody);

        var response =
            await _httpClient.SendAsync(httpRequest);

        return response.IsSuccessStatusCode;
    }


    // =========================================================
    // DELETE ITEM
    // =========================================================

    public async Task<bool> RemoveItemAsync(
        long cartItemId)
    {
        var accessToken =
            await _tokenStorage.GetAccessTokenAsync();

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return false;
        }

        using var request =
            new HttpRequestMessage(
                HttpMethod.Delete,
                $"api/cart/items/{cartItemId}");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        var response =
            await _httpClient.SendAsync(request);

        return response.IsSuccessStatusCode;
    }


    // =========================================================
    // CLEAR CART
    // =========================================================

    public async Task<bool> ClearCartAsync()
    {
        var accessToken =
            await _tokenStorage.GetAccessTokenAsync();

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return false;
        }

        using var request =
            new HttpRequestMessage(
                HttpMethod.Delete,
                "api/cart");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        var response =
            await _httpClient.SendAsync(request);

        return response.IsSuccessStatusCode;
    }
}