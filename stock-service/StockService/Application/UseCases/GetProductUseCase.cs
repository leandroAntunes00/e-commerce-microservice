using StockService.Application.DTOs;
using StockService.Domain.Interfaces;

namespace StockService.Application.UseCases;

public interface IGetProductUseCase
{
    Task<GetProductResult> ExecuteAsync(GetProductQuery query);
}

public class GetProductUseCase : IGetProductUseCase
{
    private readonly IProductRepository _productRepository;

    public GetProductUseCase(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<GetProductResult> ExecuteAsync(GetProductQuery query)
    {
        var product = await _productRepository.GetByIdAsync(query.ProductId);

        if (product == null || !product.IsActive)
        {
            return new GetProductResult
            {
                Success = false,
                Message = "Product not found"
            };
        }

        return new GetProductResult
        {
            Success = true,
            Message = "Product retrieved successfully",
            Product = ProductDto.FromEntity(product)
        };
    }
}

public class GetProductResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public ProductDto? Product { get; set; }
}
