using StockService.Application.Dtos;
using StockService.Domain.Interfaces;

namespace StockService.Application.UseCases;

public interface IUpdateStockUseCase
{
    Task<UpdateStockResult> ExecuteAsync(UpdateStockCommand command);
}

public class UpdateStockUseCase : IUpdateStockUseCase
{
    private readonly IProductRepository _productRepository;

    public UpdateStockUseCase(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<UpdateStockResult> ExecuteAsync(UpdateStockCommand command)
    {
        // Verificar se produto existe
        var product = await _productRepository.GetByIdAsync(command.ProductId);
        if (product == null || !product.IsActive)
        {
            return new UpdateStockResult
            {
                Success = false,
                Message = "Product not found"
            };
        }

        // Validar nova quantidade
        if (command.NewStockQuantity < 0)
        {
            return new UpdateStockResult
            {
                Success = false,
                Message = "Stock quantity cannot be negative"
            };
        }

        // Atualizar quantidade
        product.StockQuantity = command.NewStockQuantity;
        await _productRepository.UpdateAsync(product);

        return new UpdateStockResult
        {
            Success = true,
            Message = "Stock updated successfully",
            PreviousStock = product.StockQuantity - command.NewStockQuantity,
            NewStock = command.NewStockQuantity
        };
    }
}

public class UpdateStockResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int PreviousStock { get; set; }
    public int NewStock { get; set; }
}
