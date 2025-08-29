using System.Net.Http.Json;

namespace SalesService.Services;

public interface IStockServiceClient
{
    Task<bool> CheckStockAvailability(int productId, int quantity);
    Task<bool> ReserveStock(int productId, int quantity);
    Task<bool> ReleaseStock(int productId, int quantity);
    Task<StockProductResponse?> GetProductAsync(int productId);
}

public class StockServiceClient : IStockServiceClient
{
    private readonly HttpClient _httpClient;

    public StockServiceClient(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("StockService");
    }

    public async Task<bool> CheckStockAvailability(int productId, int quantity)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/stock/products/{productId}");

            if (!response.IsSuccessStatusCode)
                return false;

            var envelope = await response.Content.ReadFromJsonAsync<StockServiceEnvelope>();
            var product = envelope?.Product;
            return product != null && product.StockQuantity >= quantity;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ReserveStock(int productId, int quantity)
    {
        try
        {
            var resp = await _httpClient.PostAsJsonAsync($"/api/stock/products/{productId}/reserve", new { Quantity = quantity });
            if (!resp.IsSuccessStatusCode) return false;
            var envelope = await resp.Content.ReadFromJsonAsync<StockServiceEnvelope>();
            return envelope != null && envelope.Success;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ReleaseStock(int productId, int quantity)
    {
        try
        {
            var resp = await _httpClient.PostAsJsonAsync($"/api/stock/products/{productId}/release", new { Quantity = quantity });
            if (!resp.IsSuccessStatusCode) return false;
            var envelope = await resp.Content.ReadFromJsonAsync<StockServiceEnvelope>();
            return envelope != null && envelope.Success;
        }
        catch
        {
            return false;
        }
    }

    public async Task<StockProductResponse?> GetProductAsync(int productId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/stock/products/{productId}");
            if (!response.IsSuccessStatusCode) return null;
            var envelope = await response.Content.ReadFromJsonAsync<StockServiceEnvelope>();
            return envelope?.Product;
        }
        catch
        {
            return null;
        }
    }
}

public class StockProductResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
}

public class StockServiceEnvelope
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public StockProductResponse? Product { get; set; }
}
