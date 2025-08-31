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
    private readonly AutoMapper.IMapper _mapper;

    public GetProductsUseCase(IProductRepository productRepository, AutoMapper.IMapper mapper)
    {
        _productRepository = productRepository;
        _mapper = mapper;
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

        var productDtos = _mapper.Map<List<ProductDto>>(products);

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
