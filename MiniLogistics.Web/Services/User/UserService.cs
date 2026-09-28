
using System.Net.Http.Headers;
using System.Net.Http.Json;

using MiniLogistics.Web.Models.User;
using MiniLogistics.Web.Services.Auth;

namespace MiniLogistics.Web.Services.User;

public class UserService
{
    private readonly HttpClient _httpClient;
    private readonly TokenStorageService _tokenStorage;

    public UserService(
        HttpClient httpClient,
        TokenStorageService tokenStorage)
    {
        _httpClient = httpClient;
        _tokenStorage = tokenStorage;
    }

    // GET /api/users/me
    // Lấy thông tin profile của user đang đăng nhập
    public async Task<UserProfile?> GetMyProfileAsync()
    {
        using var request = await CreateAuthorizedRequestAsync(
            HttpMethod.Get,
            "api/users/me");

        if (request == null)
            return null;

        using var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<UserProfile>();
    }

    // PUT /api/users/me
    // Cập nhật thông tin profile
    public async Task<UserProfile?> UpdateMyProfileAsync(
        UpdateMyProfileRequest model)
    {
        using var request = await CreateAuthorizedRequestAsync(
            HttpMethod.Put,
            "api/users/me");

        if (request == null)
            return null;

        request.Content = JsonContent.Create(model);

        using var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<UserProfile>();
    }

    // POST /api/users/me/avatar
    // Upload ảnh đại diện từ máy tính
    public async Task<UploadAvatarResponse?> UploadAvatarAsync(
        Stream fileStream,
        string fileName,
        string contentType)
    {
        using var request = await CreateAuthorizedRequestAsync(
            HttpMethod.Post,
            "api/users/me/avatar");

        if (request == null)
            return null;

        using var content = new MultipartFormDataContent();
        using var fileContent = new StreamContent(fileStream);

        fileContent.Headers.ContentType =
            new MediaTypeHeaderValue(contentType);

        // Tên field "file" phải trùng với tham số IFormFile file ở API
        content.Add(fileContent, "file", fileName);

        request.Content = content;

        using var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content
            .ReadFromJsonAsync<UploadAvatarResponse>();
    }

    // Tạo HTTP request kèm JWT
    private async Task<HttpRequestMessage?> CreateAuthorizedRequestAsync(
        HttpMethod method,
        string url)
    {
        var accessToken = await _tokenStorage.GetAccessTokenAsync();

        if (string.IsNullOrWhiteSpace(accessToken))
            return null;

        var request = new HttpRequestMessage(method, url);

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        return request;
    }
}