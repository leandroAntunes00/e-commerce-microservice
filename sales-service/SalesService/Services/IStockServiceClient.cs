using System.Net.Http.Json;

namespace SalesService.Services;

public interface IStockServiceClient
{
    Task<bool> CheckStockAvailability(int productId, int quantity);
    Task<bool> ReserveStock(int productId, int quantity);
    Task<bool> ReleaseStock(int productId, int quantity);
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

            var product = await response.Content.ReadFromJsonAsync<StockProductResponse>();
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
            // This would typically call a specific endpoint for stock reservation
            // For now, we'll just check availability
            return await CheckStockAvailability(productId, quantity);
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
            // This would typically call an endpoint to release reserved stock
            // For now, we'll return true as this is a simplified implementation
            return await Task.FromResult(true);
        }
        catch
        {
            return false;
        }
    }
}

public class StockProductResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
}
