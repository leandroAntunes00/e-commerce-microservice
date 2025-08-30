using StockService.Application.DTOs;
using StockService.Domain.Interfaces;

namespace StockService.Application.UseCases;

public interface IGetProductsUseCase
{
    Task<GetProductsResult> ExecuteAsync(GetProductsQuery query);
}

public class GetProductsUseCase : IGetProductsUseCase
{
    private readonly IProductRepository _productRepository;

    public GetProductsUseCase(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<GetProductsResult> ExecuteAsync(GetProductsQuery query)
    {
        IEnumerable<Domain.Entities.Product> products;

        if (!string.IsNullOrEmpty(query.Category))
        {
            products = await _productRepository.GetByCategoryAsync(query.Category);
        }
        else if (!string.IsNullOrEmpty(query.SearchTerm))
        {
            products = await _productRepository.SearchAsync(query.SearchTerm);
        }
        else
        {
            products = await _productRepository.GetAllActiveAsync();
        }

        var productDtos = products.Select(ProductDto.FromEntity).ToList();

        return new GetProductsResult
        {
            Products = productDtos,
            TotalCount = productDtos.Count
        };
    }
}

public class GetProductsResult
{
    public List<ProductDto> Products { get; set; } = new();
    public int TotalCount { get; set; }
}
