
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MiniLogistics.Web.Models.Review;
using MiniLogistics.Web.Services.Auth;

namespace MiniLogistics.Web.Services.Review;

public class ReviewService
{
    private readonly HttpClient _httpClient;
    private readonly TokenStorageService _tokenStorage;

    public ReviewService(
        HttpClient httpClient,
        TokenStorageService tokenStorage)
    {
        _httpClient = httpClient;
        _tokenStorage = tokenStorage;
    }

    private async Task<string?> GetAccessTokenAsync()
    {
        return await _tokenStorage.GetAccessTokenAsync();
    }

    // GET: api/review/my
    public async Task<ReviewListResponse?> GetMyReviewsAsync(
        string? search = null,
        int? rating = null,
        int page = 1,
        int pageSize = 10)
    {
        var token = await GetAccessTokenAsync();

        if (string.IsNullOrWhiteSpace(token))
            return null;

        var query = new List<string>
        {
            $"page={page}",
            $"pageSize={pageSize}"
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            query.Add(
                $"search={Uri.EscapeDataString(search)}");
        }

        if (rating.HasValue)
        {
            query.Add($"rating={rating.Value}");
        }

        var url = $"api/review/my?{string.Join("&", query)}";

        using var request = new HttpRequestMessage(
            HttpMethod.Get, url);

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content
            .ReadFromJsonAsync<ReviewListResponse>();
    }

    // POST: api/review
    public async Task<bool> CreateReviewAsync(
        CreateReviewRequest review)
    {
        var token = await GetAccessTokenAsync();

        if (string.IsNullOrWhiteSpace(token))
            return false;

        using var request = new HttpRequestMessage(
            HttpMethod.Post, "api/review");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        request.Content = JsonContent.Create(review);

        var response = await _httpClient.SendAsync(request);

        return response.IsSuccessStatusCode;
    }

    // PUT: api/review/{id}
    public async Task<bool> UpdateReviewAsync(
        long id,
        UpdateReviewRequest review)
    {
        var token = await GetAccessTokenAsync();

        if (string.IsNullOrWhiteSpace(token))
            return false;

        using var request = new HttpRequestMessage(
            HttpMethod.Put, $"api/review/{id}");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        request.Content = JsonContent.Create(review);

        var response = await _httpClient.SendAsync(request);

        return response.IsSuccessStatusCode;
    }

    // DELETE: api/review/{id}
    public async Task<bool> DeleteReviewAsync(long id)
    {
        var token = await GetAccessTokenAsync();

        if (string.IsNullOrWhiteSpace(token))
            return false;

        using var request = new HttpRequestMessage(
            HttpMethod.Delete, $"api/review/{id}");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);

        return response.IsSuccessStatusCode;
    }
}