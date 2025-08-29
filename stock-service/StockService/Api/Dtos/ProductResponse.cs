using StockService.Application.DTOs;

namespace StockService.Api.Dtos;

public class ProductResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public ProductDto? Product { get; set; }
    public List<ProductDto> Products { get; set; } = new();
    public int TotalCount { get; set; }
}

public class StockUpdateResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int PreviousStock { get; set; }
    public int NewStock { get; set; }
}

public class CreateProductResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int ProductId { get; set; }
}
