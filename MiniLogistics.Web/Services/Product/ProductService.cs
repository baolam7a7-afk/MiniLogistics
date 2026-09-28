using System.Net.Http.Json;
using MiniLogistics.Web.Models.Product;

namespace MiniLogistics.Web.Services.Product;

public class ProductService
{
    private readonly HttpClient _httpClient;

    public ProductService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ProductListResponse?> GetProductsAsync(
        string? search = null,
        long? categoryId = null,
        long? shopId = null,
        string? status = null,
        int page = 1,
        int pageSize = 12)
    {
        var queryParams = new List<string>();

        if (!string.IsNullOrWhiteSpace(search))
        {
            queryParams.Add(
                $"Search={Uri.EscapeDataString(search)}");
        }

        if (categoryId.HasValue)
        {
            queryParams.Add(
                $"CategoryId={categoryId.Value}");
        }

        if (shopId.HasValue)
        {
            queryParams.Add(
                $"ShopId={shopId.Value}");
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            queryParams.Add(
                $"Status={Uri.EscapeDataString(status)}");
        }

        queryParams.Add($"Page={page}");
        queryParams.Add($"PageSize={pageSize}");

        var url = "api/products";

        if (queryParams.Count > 0)
        {
            url += "?" + string.Join("&", queryParams);
        }

        var response =
            await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content
            .ReadFromJsonAsync<ProductListResponse>();
    }


    public async Task<ProductItem?> GetProductByIdAsync(
        long id)
    {
        var response =
            await _httpClient.GetAsync(
                $"api/products/{id}");

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content
            .ReadFromJsonAsync<ProductItem>();
    }


    public async Task<List<ProductVariant>?>
        GetVariantsByProductIdAsync(long productId)
    {
        var response =
            await _httpClient.GetAsync(
                $"api/product-variants/product/{productId}");

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content
            .ReadFromJsonAsync<List<ProductVariant>>();
    }


    public async Task<List<ProductImage>>
        GetImagesByProductIdAsync(long productId)
    {
        var response = await _httpClient.GetAsync(
            $"api/product-images/product/{productId}");

        if (!response.IsSuccessStatusCode)
        {
            return new List<ProductImage>();
        }

        return await response.Content
            .ReadFromJsonAsync<List<ProductImage>>()
            ?? new List<ProductImage>();
    }
}