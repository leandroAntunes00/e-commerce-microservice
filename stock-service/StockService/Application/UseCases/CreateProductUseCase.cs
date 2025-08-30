using StockService.Application.DTOs;
using StockService.Domain.Entities;
using StockService.Domain.Interfaces;

namespace StockService.Application.UseCases;

public interface ICreateProductUseCase
{
    Task<CreateProductResult> ExecuteAsync(CreateProductCommand command);
}

public class CreateProductUseCase : ICreateProductUseCase
{
    private readonly IProductRepository _productRepository;

    public CreateProductUseCase(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<CreateProductResult> ExecuteAsync(CreateProductCommand command)
    {
        // Validações de negócio
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return new CreateProductResult
            {
                Success = false,
                Message = "Product name is required"
            };
        }

        if (command.Price <= 0)
        {
            return new CreateProductResult
            {
                Success = false,
                Message = "Product price must be greater than zero"
            };
        }

        if (command.StockQuantity < 0)
        {
            return new CreateProductResult
            {
                Success = false,
                Message = "Stock quantity cannot be negative"
            };
        }

        // Criar entidade
        var product = new Product
        {
            Name = command.Name.Trim(),
            Description = command.Description?.Trim() ?? string.Empty,
            Price = command.Price,
            Category = command.Category.Trim(),
            StockQuantity = command.StockQuantity,
            ImageUrl = command.ImageUrl?.Trim() ?? string.Empty,
            IsActive = true
        };

        // Persistir
        var createdProduct = await _productRepository.CreateAsync(product);

        return new CreateProductResult
        {
            Success = true,
            Message = "Product created successfully",
            ProductId = createdProduct.Id
        };
    }
}

public class CreateProductResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int ProductId { get; set; }
}
