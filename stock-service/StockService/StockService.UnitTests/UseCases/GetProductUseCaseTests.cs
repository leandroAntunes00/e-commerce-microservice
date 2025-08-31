using Xunit;
using Moq;
using FluentAssertions;
using AutoMapper;
using StockService.Application.UseCases;
using StockService.Domain.Interfaces;
using StockService.Domain.Entities;
using StockService.Application.DTOs;
using StockService.Application.Mapping;

namespace StockService.UnitTests.UseCases;

public class GetProductUseCaseTests
{
    private readonly IMapper _mapper;

    public GetProductUseCaseTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile(new ProductProfile()));
        _mapper = config.CreateMapper();
    }

    [Fact]
    public async Task ExecuteAsync_ProductNotFound_ReturnsError()
    {
        // Arrange
        var repoMock = new Mock<IProductRepository>();
        repoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Product?)null);

        var useCase = new GetProductUseCase(repoMock.Object, _mapper);

        var query = new GetProductQuery { ProductId = 10 };

        // Act
        var result = await useCase.ExecuteAsync(query);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task ExecuteAsync_ProductFound_ReturnsProduct()
    {
        // Arrange
        var product = new Product { Id = 5, Name = "Found", IsActive = true };
        var repoMock = new Mock<IProductRepository>();
        repoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(product);

        var useCase = new GetProductUseCase(repoMock.Object, _mapper);

        var query = new GetProductQuery { ProductId = 5 };

        // Act
        var result = await useCase.ExecuteAsync(query);

        // Assert
        result.Success.Should().BeTrue();
        result.Product.Should().NotBeNull();
        result.Product!.Id.Should().Be(5);
    }
}
