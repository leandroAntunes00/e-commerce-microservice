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

public class GetProductsUseCaseTests
{
    private readonly IMapper _mapper;

    public GetProductsUseCaseTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile(new ProductProfile()));
        _mapper = config.CreateMapper();
    }

    [Fact]
    public async Task ExecuteAsync_ByCategory_ReturnsMappedProducts()
    {
        // Arrange
        var products = new List<Product>
        {
            new Product { Id = 1, Name = "P1", Category = "C1", IsActive = true }
        };

        var repoMock = new Mock<IProductRepository>();
        repoMock.Setup(r => r.GetByCategoryAsync("C1")).ReturnsAsync(products);

        var useCase = new GetProductsUseCase(repoMock.Object, _mapper);

        var query = new GetProductsQuery { Category = "C1" };

        // Act
        var result = await useCase.ExecuteAsync(query);

        // Assert
        result.Products.Should().HaveCount(1);
        result.Products[0].Id.Should().Be(1);
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_BySearch_ReturnsMappedProducts()
    {
        // Arrange
        var products = new List<Product>
        {
            new Product { Id = 2, Name = "SearchMatch", Category = "C2", IsActive = true }
        };

        var repoMock = new Mock<IProductRepository>();
        repoMock.Setup(r => r.SearchAsync("term")).ReturnsAsync(products);

        var useCase = new GetProductsUseCase(repoMock.Object, _mapper);

        var query = new GetProductsQuery { SearchTerm = "term" };

        // Act
        var result = await useCase.ExecuteAsync(query);

        // Assert
        result.Products.Should().HaveCount(1);
        result.Products[0].Name.Should().Be("SearchMatch");
    }

    [Fact]
    public async Task ExecuteAsync_NoFilter_ReturnsAllActive()
    {
        // Arrange
        var products = new List<Product>
        {
            new Product { Id = 3, Name = "A", Category = "C", IsActive = true },
            new Product { Id = 4, Name = "B", Category = "C", IsActive = true }
        };

        var repoMock = new Mock<IProductRepository>();
        repoMock.Setup(r => r.GetAllActiveAsync()).ReturnsAsync(products);

        var useCase = new GetProductsUseCase(repoMock.Object, _mapper);

        var query = new GetProductsQuery();

        // Act
        var result = await useCase.ExecuteAsync(query);

        // Assert
        result.Products.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }
}
