using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using StockService.Api.Dtos;
using StockService.Application.UseCases;
using StockService.Application.DTOs;
using Messaging;

namespace StockService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockController : ControllerBase
{
    private readonly IGetProductsUseCase _getProductsUseCase;
    private readonly IGetProductUseCase _getProductUseCase;
    private readonly ICreateProductUseCase _createProductUseCase;
    private readonly IUpdateStockUseCase _updateStockUseCase;
    private readonly IMessagePublisher _messagePublisher;
    private readonly AutoMapper.IMapper _mapper;

    public StockController(
        IGetProductsUseCase getProductsUseCase,
        IGetProductUseCase getProductUseCase,
        ICreateProductUseCase createProductUseCase,
        IUpdateStockUseCase updateStockUseCase,
    IMessagePublisher messagePublisher,
    AutoMapper.IMapper mapper)
    {
        _getProductsUseCase = getProductsUseCase;
        _getProductUseCase = getProductUseCase;
        _createProductUseCase = createProductUseCase;
        _updateStockUseCase = updateStockUseCase;
        _messagePublisher = messagePublisher;
        _mapper = mapper;
    }

    // GET: api/stock/products - Lista todos os produtos ativos (público)
    [HttpGet("products")]
    public async Task<IActionResult> GetProducts([FromQuery] string? category = null, [FromQuery] string? search = null)
    {
        try
        {
            var query = new GetProductsQuery { Category = category, SearchTerm = search };
            var result = await _getProductsUseCase.ExecuteAsync(query);

            return Ok(new ProductResponse
            {
                Success = true,
                Message = "Products retrieved successfully",
                Products = result.Products,
                TotalCount = result.TotalCount
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ProductResponse
            {
                Success = false,
                Message = $"Failed to retrieve products: {ex.Message}"
            });
        }
    }

    // GET: api/stock/products/{id} - Obtém produto por ID (público)
    [HttpGet("products/{id}")]
    public async Task<IActionResult> GetProduct(int id)
    {
        try
        {
            var query = new GetProductQuery { ProductId = id };
            var result = await _getProductUseCase.ExecuteAsync(query);

            if (!result.Success)
            {
                return NotFound(new ProductResponse
                {
                    Success = false,
                    Message = result.Message
                });
            }

            return Ok(new ProductResponse
            {
                Success = true,
                Message = result.Message,
                Product = result.Product
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ProductResponse
            {
                Success = false,
                Message = $"Failed to retrieve product: {ex.Message}"
            });
        }
    }

    // GET: api/stock/products/category/{category} - Lista produtos por categoria (público)
    [HttpGet("products/category/{category}")]
    public async Task<IActionResult> GetProductsByCategory(string category)
    {
        try
        {
            var query = new GetProductsQuery { Category = category };
            var result = await _getProductsUseCase.ExecuteAsync(query);

            return Ok(new ProductResponse
            {
                Success = true,
                Message = $"Products in category '{category}' retrieved successfully",
                Products = result.Products,
                TotalCount = result.TotalCount
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ProductResponse
            {
                Success = false,
                Message = $"Failed to retrieve products by category: {ex.Message}"
            });
        }
    }

    // POST: api/stock/products - Cria produto (requer autenticação de Admin)
    [HttpPost("products")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
    {
        try
        {
            var command = _mapper.Map<CreateProductCommand>(request);
            var result = await _createProductUseCase.ExecuteAsync(command);

            if (!result.Success)
            {
                return BadRequest(new CreateProductResponse
                {
                    Success = false,
                    Message = result.Message
                });
            }

            return CreatedAtAction(nameof(GetProduct), new { id = result.ProductId },
                new CreateProductResponse
                {
                    Success = true,
                    Message = result.Message,
                    ProductId = result.ProductId
                });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new CreateProductResponse
            {
                Success = false,
                Message = $"Failed to create product: {ex.Message}"
            });
        }
    }

    // PUT: api/stock/products/{id}/stock - Atualiza quantidade em estoque (somente Admin)
    [HttpPut("products/{id}/stock")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> UpdateStock(int id, [FromBody] UpdateStockRequest request)
    {
        try
        {
            var command = _mapper.Map<UpdateStockCommand>(request);
            command.ProductId = id;
            var result = await _updateStockUseCase.ExecuteAsync(command);

            if (!result.Success)
            {
                return BadRequest(new StockUpdateResponse
                {
                    Success = false,
                    Message = result.Message
                });
            }

            return Ok(new StockUpdateResponse
            {
                Success = true,
                Message = result.Message,
                PreviousStock = result.PreviousStock,
                NewStock = result.NewStock
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new StockUpdateResponse
            {
                Success = false,
                Message = $"Failed to update stock: {ex.Message}"
            });
        }
    }

    // GET: api/stock/health - Health check
    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok("Stock Service is running!");
    }
}
