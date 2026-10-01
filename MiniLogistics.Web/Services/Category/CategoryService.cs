using System.Net.Http.Json;
using MiniLogistics.Web.Models.Category;

namespace MiniLogistics.Web.Services.Category;

public class CategoryService
{
    private readonly HttpClient _httpClient;

    public CategoryService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<CategoryItem>> GetActiveCategoriesAsync()
    {
        var url = "api/categories?Page=1&PageSize=100&IsActive=true";
        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            return new List<CategoryItem>();

        var result = await response.Content
            .ReadFromJsonAsync<CategoryListResponse>();

        return result?.Items?
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToList()
            ?? new List<CategoryItem>();
    }
}
