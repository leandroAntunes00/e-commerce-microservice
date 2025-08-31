using Xunit;
using Moq;
using FluentAssertions;
using StockService.Application.UseCases;
using StockService.Domain.Interfaces;
using StockService.Domain.Entities;
using StockService.Application.DTOs;

namespace StockService.UnitTests.UseCases;

public class CreateProductUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WithValidCommand_CreatesProduct()
    {
        // Arrange
        var repoMock = new Mock<IProductRepository>();
        repoMock.Setup(r => r.CreateAsync(It.IsAny<Product>()))
            .ReturnsAsync((Product p) => { p.Id = 42; return p; });

        var useCase = new CreateProductUseCase(repoMock.Object);

        var command = new CreateProductCommand
        {
            Name = "Test",
            Description = "Desc",
            Price = 10.5m,
            Category = "Cat",
            StockQuantity = 5,
            ImageUrl = null!
        };

        // Act
        var result = await useCase.ExecuteAsync(command);

        // Assert
        result.Success.Should().BeTrue();
        result.ProductId.Should().Be(42);
        repoMock.Verify(r => r.CreateAsync(It.IsAny<Product>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidPrice_ReturnsError()
    {
        // Arrange
        var repoMock = new Mock<IProductRepository>();
        var useCase = new CreateProductUseCase(repoMock.Object);

        var command = new CreateProductCommand
        {
            Name = "Test",
            Description = "Desc",
            Price = 0m,
            Category = "Cat",
            StockQuantity = 5,
            ImageUrl = null!
        };

        // Act
        var result = await useCase.ExecuteAsync(command);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("price");
        repoMock.Verify(r => r.CreateAsync(It.IsAny<Product>()), Times.Never);
    }
}
