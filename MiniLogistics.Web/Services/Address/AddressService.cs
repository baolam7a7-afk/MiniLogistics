using System.Net.Http.Headers;
using System.Net.Http.Json;
using AddressModel = MiniLogistics.Web.Models.Address.Address;
using MiniLogistics.Web.Models.Address;
using MiniLogistics.Web.Services.Auth;

namespace MiniLogistics.Web.Services.Address;

public class AddressService
{
    private readonly HttpClient _httpClient;
    private readonly TokenStorageService _tokenStorage;

    public AddressService(
        HttpClient httpClient,
        TokenStorageService tokenStorage)
    {
        _httpClient = httpClient;
        _tokenStorage = tokenStorage;
    }

    // =========================================================
    // GET ALL ADDRESSES
    // =========================================================

    public async Task<List<AddressModel>?> GetAddressesAsync()
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
                "api/addresses");

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
            .ReadFromJsonAsync<List<AddressModel>>();
    }

    // =========================================================
    // GET ADDRESS BY ID
    // =========================================================

    public async Task<AddressModel?> GetAddressByIdAsync(
        long id)
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
                $"api/addresses/{id}");

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
            .ReadFromJsonAsync<AddressModel>();
    }

    // =========================================================
    // CREATE ADDRESS
    // =========================================================

    public async Task<bool> CreateAddressAsync(
        CreateAddressRequest requestBody)
    {
        var accessToken =
            await _tokenStorage.GetAccessTokenAsync();

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return false;
        }

        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                "api/addresses");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        request.Content =
            JsonContent.Create(requestBody);

        var response =
            await _httpClient.SendAsync(request);

        return response.IsSuccessStatusCode;
    }

    // =========================================================
    // UPDATE ADDRESS
    // =========================================================

    public async Task<bool> UpdateAddressAsync(
        long id,
        UpdateAddressRequest requestBody)
    {
        var accessToken =
            await _tokenStorage.GetAccessTokenAsync();

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return false;
        }

        using var request =
            new HttpRequestMessage(
                HttpMethod.Put,
                $"api/addresses/{id}");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        request.Content =
            JsonContent.Create(requestBody);

        var response =
            await _httpClient.SendAsync(request);

        return response.IsSuccessStatusCode;
    }

    // =========================================================
    // DELETE ADDRESS
    // =========================================================

    public async Task<bool> DeleteAddressAsync(
        long id)
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
                $"api/addresses/{id}");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        var response =
            await _httpClient.SendAsync(request);

        return response.IsSuccessStatusCode;
    }
}