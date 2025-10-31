using ProductStockSystem.Models;
using System.Text.Json;

namespace ProductStockSystem.McpServer.Services;

public class StockApiService
{
    private readonly HttpClient _httpClient;

    public StockApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<ProductDto>> GetProductsAsync()
    {
        var response = await _httpClient.GetAsync("api/products");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<ProductDto>>(json, JsonSerializerOptions.Web) ?? new List<ProductDto>();
    }

    public async Task<ProductDto?> GetProductByIdAsync(int id)
    {
        var response = await _httpClient.GetAsync($"api/products/{id}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ProductDto>(json, JsonSerializerOptions.Web);
    }

    public async Task<List<ProductDto>> GetLowStockProductsAsync()
    {
        var response = await _httpClient.GetAsync("api/products/low-stock");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<ProductDto>>(json, JsonSerializerOptions.Web) ?? new List<ProductDto>();
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductRequest request)
    {
        var requestJson = JsonSerializer.Serialize(request, JsonSerializerOptions.Web);
        var content = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("api/products", content);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ProductDto>(json, JsonSerializerOptions.Web)!;
    }

    public async Task<ProductDto> UpdateStockAsync(int productId, UpdateStockRequest request)
    {
        var requestJson = JsonSerializer.Serialize(request, JsonSerializerOptions.Web);
        var content = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.PutAsync($"api/products/{productId}/stock", content);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ProductDto>(json, JsonSerializerOptions.Web)!;
    }

    public async Task<List<StockMovementDto>> GetProductMovementsAsync(int productId)
    {
        var response = await _httpClient.GetAsync($"api/products/{productId}/movements");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<StockMovementDto>>(json, JsonSerializerOptions.Web) ?? new List<StockMovementDto>();
    }

    public async Task<List<StockMovementDto>> GetAllMovementsAsync()
    {
        var response = await _httpClient.GetAsync("api/movements");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<StockMovementDto>>(json, JsonSerializerOptions.Web) ?? new List<StockMovementDto>();
    }

    public async Task<bool> IsApiHealthyAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}