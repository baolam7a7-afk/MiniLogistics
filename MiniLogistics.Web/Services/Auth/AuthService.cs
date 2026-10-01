using System.Net.Http.Json;
using MiniLogistics.Web.Models.Auth;

namespace MiniLogistics.Web.Services.Auth;

public class AuthService
{
    private readonly HttpClient _httpClient;

    public AuthService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // =========================================================
    // LOGIN
    // =========================================================

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/auth/login",
            request);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content
            .ReadFromJsonAsync<AuthResponse>();
    }

    // =========================================================
    // REGISTER
    // =========================================================

    public async Task<AuthResponse?> RegisterAsync(
        RegisterRequest request)
    {
        var payload = new
        {
            request.Email,
            request.Password,
            request.Phone,
            request.FullName,
            request.Role
        };

        var response = await _httpClient.PostAsJsonAsync(
            "api/auth/register",
            payload);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content
            .ReadFromJsonAsync<AuthResponse>();
    }

    // =========================================================
    // GOOGLE LOGIN
    // =========================================================

    public async Task<AuthResponse?> GoogleLoginAsync(
        GoogleLoginRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/auth/google",
            request);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content
            .ReadFromJsonAsync<AuthResponse>();
    }

    // =========================================================
    // FORGOT PASSWORD
    // =========================================================

    public async Task<ForgotPasswordResult> ForgotPasswordAsync(
        ForgotPasswordRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/auth/forgot-password",
            request);

        if (!response.IsSuccessStatusCode)
        {
            string? error = null;
            try
            {
                var body = await response.Content.ReadFromJsonAsync<AuthMessage>();
                error = body?.Message;
            }
            catch (System.Text.Json.JsonException)
            {
            }

            return new ForgotPasswordResult
            {
                Error = error ?? "Không tạo được yêu cầu đặt lại mật khẩu."
            };
        }

        var ok = await response.Content.ReadFromJsonAsync<AuthMessage>();
        return new ForgotPasswordResult
        {
            Ok = true,
            Message = ok?.Message,
            ResetToken = ok?.ResetToken
        };
    }

    // =========================================================
    // RESET PASSWORD
    // =========================================================

    public async Task<bool> ResetPasswordAsync(
        ResetPasswordRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/auth/reset-password",
            request);

        return response.IsSuccessStatusCode;
    }

    // =========================================================
    // CHANGE PASSWORD
    // =========================================================


    public async Task<bool> ChangePasswordAsync(
        ChangePasswordRequest request,
        string accessToken)
    {
        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "api/auth/change-password");

        httpRequest.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        httpRequest.Content = JsonContent.Create(request);

        var response = await _httpClient.SendAsync(httpRequest);

        return response.IsSuccessStatusCode;
    }
}