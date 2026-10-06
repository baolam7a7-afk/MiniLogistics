using System.Net.Http.Headers;
using System.Net.Http.Json;
using MiniLogistics.Web.Models.Order;
using MiniLogistics.Web.Services.Auth;

using OrderModel = MiniLogistics.Web.Models.Order.Order;

namespace MiniLogistics.Web.Services.Order;

public class OrderService
{
    private readonly HttpClient _httpClient;
    private readonly TokenStorageService _tokenStorage;

    public OrderService(
        HttpClient httpClient,
        TokenStorageService tokenStorage)
    {
        _httpClient = httpClient;
        _tokenStorage = tokenStorage;
    }

    // =========================================================
    // CREATE ORDER
    // =========================================================

    public async Task<OrderModel?> CreateOrderAsync(
    CreateOrderRequest requestBody)
    {
        var accessToken =
            await _tokenStorage.GetAccessTokenAsync();

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                "api/orders");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        request.Content =
            JsonContent.Create(requestBody);

        var response =
            await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                ReadErrorMessage(body)
                ?? "Không thể tạo đơn hàng. Vui lòng thử lại.");
        }

        return await response.Content
    .ReadFromJsonAsync<OrderModel>();
    }

    private static string? ReadErrorMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("message", out var message))
            {
                var text = message.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text;
                }
            }
        }
        catch (System.Text.Json.JsonException)
        {
        }

        return null;
    }


    // =========================================================
    // GET MY ORDERS
    // =========================================================

    public async Task<OrderListResponse?> GetMyOrdersAsync(
    string? search = null,
    string? status = null,
    int page = 1,
    int pageSize = 10)
    {
        var accessToken =
            await _tokenStorage.GetAccessTokenAsync();

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        var queryParams = new List<string>
    {
        $"page={page}",
        $"pageSize={pageSize}"
    };

        if (!string.IsNullOrWhiteSpace(search))
        {
            queryParams.Add(
                $"search={Uri.EscapeDataString(search)}");
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            queryParams.Add(
                $"status={Uri.EscapeDataString(status)}");
        }

        var url =
            $"api/orders/my?{string.Join("&", queryParams)}";

        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                url);

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
            .ReadFromJsonAsync<OrderListResponse>();
    }


    // =========================================================
    // GET ORDER BY ID
    // =========================================================

    public async Task<OrderModel?> GetOrderByIdAsync(
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
                $"api/orders/{id}");

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
    .ReadFromJsonAsync<OrderModel>();
    }


    // =========================================================
    // CANCEL ORDER
    // =========================================================

    public async Task<bool> CancelOrderAsync(
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
                HttpMethod.Post,
                $"api/orders/{id}/cancel");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        var response =
            await _httpClient.SendAsync(request);

        return response.IsSuccessStatusCode;
    }


    public async Task<OrderModel?> ConfirmReceivedAsync(long id)
    {
        var accessToken = await _tokenStorage.GetAccessTokenAsync();
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"api/orders/{id}/received");
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                ReadErrorMessage(body) ?? "Không thể xác nhận đã nhận hàng.");
        }

        return await response.Content.ReadFromJsonAsync<OrderModel>();
    }

    public async Task ApplyVoucherAsync(long orderId, string code)
    {
        var accessToken = await _tokenStorage.GetAccessTokenAsync();
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException("Phiên đăng nhập đã hết.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/order-vouchers/apply");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = JsonContent.Create(new { orderId, code });
        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                ReadErrorMessage(body) ?? "Không áp dụng được voucher.");
        }
    }
}