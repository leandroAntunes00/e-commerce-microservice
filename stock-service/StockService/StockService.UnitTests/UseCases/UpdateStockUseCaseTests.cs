using Xunit;
using Moq;
using FluentAssertions;
using StockService.Application.UseCases;
using StockService.Domain.Interfaces;
using StockService.Domain.Entities;
using StockService.Application.DTOs;

namespace StockService.UnitTests.UseCases;

public class UpdateStockUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ProductNotFound_ReturnsError()
    {
        // Arrange
        var repoMock = new Mock<IProductRepository>();
        repoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Product?)null);

        var useCase = new UpdateStockUseCase(repoMock.Object);

        var command = new UpdateStockCommand { ProductId = 1, NewStockQuantity = 5 };

        // Act
        var result = await useCase.ExecuteAsync(command);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
        repoMock.Verify(r => r.UpdateAsync(It.IsAny<Product>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ValidUpdate_UpdatesStock()
    {
        // Arrange
        var product = new Product { Id = 1, StockQuantity = 2, IsActive = true };
        var repoMock = new Mock<IProductRepository>();
        repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);
        repoMock.Setup(r => r.UpdateAsync(It.IsAny<Product>())).Returns(Task.CompletedTask).Verifiable();

        var useCase = new UpdateStockUseCase(repoMock.Object);

        var command = new UpdateStockCommand { ProductId = 1, NewStockQuantity = 10 };

        // Act
        var result = await useCase.ExecuteAsync(command);

        // Assert
        result.Success.Should().BeTrue();
        repoMock.Verify(r => r.UpdateAsync(It.Is<Product>(p => p.StockQuantity == 10)), Times.Once);
    }
}
