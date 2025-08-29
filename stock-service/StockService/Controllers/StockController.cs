using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using StockService.Domain.Entities;
using StockService.Application.DTOs;
using StockService.Data;
using System.ComponentModel.DataAnnotations;
using Messaging;
using Messaging.Events;

namespace StockService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockController : ControllerBase
{
    private readonly StockDbContext _context;
    private readonly IMessagePublisher _messagePublisher;

    public StockController(StockDbContext context, IMessagePublisher messagePublisher)
    {
        _context = context;
        _messagePublisher = messagePublisher;
    }

    // GET: api/stock/products - Lista todos os produtos ativos (público)
    [HttpGet("products")]
    public IActionResult GetProducts()
    {
        try
        {
            var products = _context.Products
                .Where(p => p.IsActive)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Description,
                    p.Price,
                    p.Category,
                    p.StockQuantity,
                    p.ImageUrl,
                    p.CreatedAt
                })
                .ToList();

            return Ok(new ProductResponse
            {
                Success = true,
                Message = "Products retrieved successfully",
                Products = products.Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    Category = p.Category,
                    StockQuantity = p.StockQuantity,
                    ImageUrl = p.ImageUrl,
                    CreatedAt = p.CreatedAt
                }).ToList()
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
    public IActionResult GetProduct(int id)
    {
        try
        {
            var product = _context.Products
                .Where(p => p.IsActive && p.Id == id)
                .FirstOrDefault();

            if (product == null)
            {
                return NotFound(new ProductResponse
                {
                    Success = false,
                    Message = "Product not found"
                });
            }

            return Ok(new ProductResponse
            {
                Success = true,
                Message = "Product retrieved successfully",
                Product = product is null ? null : ProductDto.FromEntity(product)
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
    public IActionResult GetProductsByCategory(string category)
    {
        try
        {
            var products = _context.Products
                .Where(p => p.IsActive && p.Category.ToLower() == category.ToLower())
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Description,
                    p.Price,
                    p.Category,
                    p.StockQuantity,
                    p.ImageUrl,
                    p.CreatedAt
                })
                .ToList();

            return Ok(new ProductResponse
            {
                Success = true,
                Message = $"Products in category '{category}' retrieved successfully",
                Products = products.Select(p => ProductDto.FromEntity(new Product
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    Category = p.Category,
                    StockQuantity = p.StockQuantity,
                    ImageUrl = p.ImageUrl,
                    CreatedAt = p.CreatedAt
                })).ToList()
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

    // POST: api/stock/products - Cria novo produto (ADMIN only)
    [Authorize(Roles = "ADMIN")]
    [HttpPost("products")]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
    {
        try
        {
            var product = new Product
            {
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                Category = request.Category,
                StockQuantity = request.StockQuantity,
                ImageUrl = request.ImageUrl,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProduct), new { id = product.Id },
                new ProductResponse
                {
                    Success = true,
                    Message = "Product created successfully",
                    Product = ProductDto.FromEntity(product)
                });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ProductResponse
            {
                Success = false,
                Message = $"Failed to create product: {ex.Message}"
            });
        }
    }

    // PUT: api/stock/products/{id} - Atualiza produto (ADMIN only)
    [Authorize(Roles = "ADMIN")]
    [HttpPut("products/{id}")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductRequest request)
    {
        try
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound(new ProductResponse
                {
                    Success = false,
                    Message = "Product not found"
                });
            }

            product.Name = request.Name;
            product.Description = request.Description;
            product.Price = request.Price;
            product.Category = request.Category;
            product.ImageUrl = request.ImageUrl;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new ProductResponse
            {
                Success = true,
                Message = "Product updated successfully",
                Product = ProductDto.FromEntity(product)
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ProductResponse
            {
                Success = false,
                Message = $"Failed to update product: {ex.Message}"
            });
        }
    }

    // PUT: api/stock/products/{id}/stock - Atualiza estoque (ADMIN only)
    [Authorize(Roles = "ADMIN")]
    [HttpPut("products/{id}/stock")]
    public async Task<IActionResult> UpdateStock(int id, [FromBody] UpdateStockRequest request)
    {
        try
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound(new ProductResponse
                {
                    Success = false,
                    Message = "Product not found"
                });
            }

            var previousStock = product.StockQuantity;
            product.StockQuantity = request.StockQuantity;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Publicar evento de atualização de estoque
            var stockUpdatedEvent = new StockUpdatedEvent
            {
                ProductId = product.Id,
                ProductName = product.Name,
                PreviousStock = previousStock,
                NewStock = product.StockQuantity,
                Operation = "Updated",
                UpdatedAt = product.UpdatedAt ?? DateTime.UtcNow
            };

            await _messagePublisher.PublishAsync(stockUpdatedEvent);

            return Ok(new ProductResponse
            {
                Success = true,
                Message = "Stock updated successfully",
                Product = ProductDto.FromEntity(product)
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ProductResponse
            {
                Success = false,
                Message = $"Failed to update stock: {ex.Message}"
            });
        }
    }

    // POST: api/stock/products/{id}/reserve - Reserva quantidade do estoque (chamado por SalesService)
    [HttpPost("products/{id}/reserve")]
    public async Task<IActionResult> ReserveStock(int id, [FromBody] ReserveStockRequest request)
    {
        try
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null || !product.IsActive)
            {
                return NotFound(new ProductResponse
                {
                    Success = false,
                    Message = "Product not found"
                });
            }

            if (request.Quantity <= 0)
            {
                return BadRequest(new ProductResponse
                {
                    Success = false,
                    Message = "Quantity must be greater than zero"
                });
            }

            if (product.StockQuantity < request.Quantity)
            {
                return BadRequest(new ProductResponse
                {
                    Success = false,
                    Message = "Insufficient stock"
                });
            }

            var previous = product.StockQuantity;
            product.StockQuantity -= request.Quantity;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Publicar evento de atualização de estoque (Reserva)
            var stockUpdatedEvent = new StockUpdatedEvent
            {
                ProductId = product.Id,
                ProductName = product.Name,
                PreviousStock = previous,
                NewStock = product.StockQuantity,
                Operation = "Reserved",
                UpdatedAt = product.UpdatedAt ?? DateTime.UtcNow
            };

            await _messagePublisher.PublishAsync(stockUpdatedEvent);

            return Ok(new ProductResponse
            {
                Success = true,
                Message = "Stock reserved successfully",
                Product = ProductDto.FromEntity(product)
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ProductResponse
            {
                Success = false,
                Message = $"Failed to reserve stock: {ex.Message}"
            });
        }
    }

    // POST: api/stock/products/{id}/release - Libera quantidade do estoque (para rollback)
    [HttpPost("products/{id}/release")]
    public async Task<IActionResult> ReleaseStock(int id, [FromBody] ReleaseStockRequest request)
    {
        try
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound(new ProductResponse
                {
                    Success = false,
                    Message = "Product not found"
                });
            }

            if (request.Quantity <= 0)
            {
                return BadRequest(new ProductResponse
                {
                    Success = false,
                    Message = "Quantity must be greater than zero"
                });
            }

            var previous = product.StockQuantity;
            product.StockQuantity += request.Quantity;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var stockUpdatedEvent = new StockUpdatedEvent
            {
                ProductId = product.Id,
                ProductName = product.Name,
                PreviousStock = previous,
                NewStock = product.StockQuantity,
                Operation = "Released",
                UpdatedAt = product.UpdatedAt ?? DateTime.UtcNow
            };

            await _messagePublisher.PublishAsync(stockUpdatedEvent);

            return Ok(new ProductResponse
            {
                Success = true,
                Message = "Stock released successfully",
                Product = ProductDto.FromEntity(product)
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ProductResponse
            {
                Success = false,
                Message = $"Failed to release stock: {ex.Message}"
            });
        }
    }

    // DELETE: api/stock/products/{id} - Desativa produto (ADMIN only)
    [Authorize(Roles = "ADMIN")]
    [HttpDelete("products/{id}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        try
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound(new ProductResponse
                {
                    Success = false,
                    Message = "Product not found"
                });
            }

            product.IsActive = false;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new ProductResponse
            {
                Success = true,
                Message = "Product deactivated successfully"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ProductResponse
            {
                Success = false,
                Message = $"Failed to delete product: {ex.Message}"
            });
        }
    }

    // GET: api/stock/categories - Lista categorias disponíveis (público)
    [HttpGet("categories")]
    public IActionResult GetCategories()
    {
        try
        {
            var categories = _context.Products
                .Where(p => p.IsActive)
                .Select(p => p.Category)
                .Distinct()
                .ToList();

            return Ok(new
            {
                Success = true,
                Message = "Categories retrieved successfully",
                Categories = categories
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Success = false,
                Message = $"Failed to retrieve categories: {ex.Message}"
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
