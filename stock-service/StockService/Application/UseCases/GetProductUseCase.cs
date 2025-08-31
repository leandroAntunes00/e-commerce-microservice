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
    private readonly AutoMapper.IMapper _mapper;

    public GetProductUseCase(IProductRepository productRepository, AutoMapper.IMapper mapper)
    {
        _productRepository = productRepository;
        _mapper = mapper;
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
            Product = _mapper.Map<ProductDto>(product)
        };
    }
}

public class GetProductResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public ProductDto? Product { get; set; }
}
