using Xunit;
using FluentAssertions;
using StockService.Domain.Entities;
using StockService.Application.DTOs;
using StockService.Api.Dtos;

namespace StockService.UnitTests;

public class ProductModelTests
{
    [Fact]
    public void Product_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var product = new Product();

        // Assert
        product.Id.Should().Be(0);
        product.Name.Should().BeEmpty();
        product.Description.Should().BeEmpty();
        product.Price.Should().Be(0);
        product.Category.Should().BeEmpty();
        product.StockQuantity.Should().Be(0);
        product.ImageUrl.Should().BeEmpty();
        product.IsActive.Should().BeTrue();
        product.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        product.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void CreateProductRequest_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var request = new CreateProductRequest();

        // Assert
        request.Name.Should().BeEmpty();
        request.Description.Should().BeEmpty();
        request.Price.Should().Be(0);
        request.Category.Should().BeEmpty();
        request.StockQuantity.Should().Be(0);
        request.ImageUrl.Should().BeEmpty();
    }

    [Fact]
    public void UpdateProductRequest_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var request = new UpdateProductRequest();

        // Assert
        request.Name.Should().BeEmpty();
        request.Description.Should().BeEmpty();
        request.Price.Should().Be(0);
        request.Category.Should().BeEmpty();
        request.ImageUrl.Should().BeEmpty();
    }

    [Fact]
    public void UpdateStockRequest_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var request = new UpdateStockRequest();

        // Assert
        request.StockQuantity.Should().Be(0);
    }

    [Fact]
    public void ProductResponse_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var response = new ProductResponse();

        // Assert
        response.Success.Should().BeFalse();
        response.Message.Should().BeEmpty();
        response.Product.Should().BeNull();
    response.Products.Should().BeEmpty();
    }
}