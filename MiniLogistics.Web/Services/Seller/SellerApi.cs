using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MiniLogistics.Web.Services.Auth;

namespace MiniLogistics.Web.Services.Seller;

public class ApiResult<T>
{
    public bool Ok { get; init; }
    public int StatusCode { get; init; }
    public T? Data { get; init; }
    public string? Error { get; init; }
}

public class SellerApi
{
    private readonly HttpClient _http;
    private readonly TokenStorageService _tokens;

    public SellerApi(HttpClient http, TokenStorageService tokens)
    {
        _http = http;
        _tokens = tokens;
    }

    public Task<ApiResult<T>> GetAsync<T>(string url) =>
        SendAsync<T>(HttpMethod.Get, url, null);

    public Task<ApiResult<T>> PostAsync<T>(string url, object? body = null) =>
        SendAsync<T>(HttpMethod.Post, url, body);

    public Task<ApiResult<T>> PutAsync<T>(string url, object? body = null) =>
        SendAsync<T>(HttpMethod.Put, url, body);

    public async Task<ApiResult<T>> UploadAsync<T>(string url, byte[] bytes, string fileName, string contentType)
    {
        var token = await _tokens.GetAccessTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
        {
            return new ApiResult<T>
            {
                StatusCode = 401,
                Error = "Phiên đăng nhập đã hết. Vui lòng đăng nhập lại."
            };
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(file, "file", fileName);
        request.Content = form;

        try
        {
            using var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return new ApiResult<T>
                {
                    StatusCode = (int)response.StatusCode,
                    Error = await ReadErrorAsync(response)
                };
            }

            var data = await response.Content.ReadFromJsonAsync<T>();
            return new ApiResult<T> { Ok = true, StatusCode = (int)response.StatusCode, Data = data };
        }
        catch (HttpRequestException)
        {
            return new ApiResult<T> { Error = "Không kết nối được API. Kiểm tra máy chủ đang chạy." };
        }
    }

    public async Task<ApiResult<bool>> DeleteAsync(string url)
    {
        var result = await SendAsync<JsonElement>(HttpMethod.Delete, url, null);
        return new ApiResult<bool>
        {
            Ok = result.Ok,
            StatusCode = result.StatusCode,
            Data = result.Ok,
            Error = result.Error
        };
    }

    private async Task<ApiResult<T>> SendAsync<T>(
        HttpMethod method,
        string url,
        object? body)
    {
        var token = await _tokens.GetAccessTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
        {
            return new ApiResult<T>
            {
                StatusCode = 401,
                Error = "Phiên đăng nhập đã hết. Vui lòng đăng nhập lại."
            };
        }

        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body != null)
        {
            request.Content = JsonContent.Create(body);
        }

        try
        {
            using var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return new ApiResult<T>
                {
                    StatusCode = (int)response.StatusCode,
                    Error = await ReadErrorAsync(response)
                };
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NoContent ||
                response.Content.Headers.ContentLength == 0)
            {
                return new ApiResult<T>
                {
                    Ok = true,
                    StatusCode = (int)response.StatusCode
                };
            }

            var data = await response.Content.ReadFromJsonAsync<T>();
            return new ApiResult<T>
            {
                Ok = true,
                StatusCode = (int)response.StatusCode,
                Data = data
            };
        }
        catch (HttpRequestException)
        {
            return new ApiResult<T>
            {
                Error = "Không kết nối được API. Kiểm tra máy chủ đang chạy."
            };
        }
        catch (JsonException)
        {
            return new ApiResult<T>
            {
                Error = "API trả dữ liệu không đúng định dạng."
            };
        }
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        try
        {
            var body = await response.Content.ReadFromJsonAsync<ApiErrorBody>();
            if (!string.IsNullOrWhiteSpace(body?.Message))
            {
                return body.Message;
            }
        }
        catch (JsonException)
        {
        }

        return response.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => "Phiên đăng nhập không hợp lệ.",
            System.Net.HttpStatusCode.Forbidden => "Tài khoản không có quyền thực hiện thao tác này.",
            System.Net.HttpStatusCode.NotFound => "Không tìm thấy dữ liệu.",
            _ => "Yêu cầu không thành công."
        };
    }

    private sealed class ApiErrorBody
    {
        public string? Message { get; set; }
    }
}
