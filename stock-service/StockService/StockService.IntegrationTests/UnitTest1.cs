using Xunit;
using FluentAssertions;
using StockService.Domain.Entities;
using StockService.Application.DTOs;
using StockService.Api.Dtos;

namespace StockService.IntegrationTests;

public class ProductModelIntegrationTests
{
    [Fact]
    public void Product_ShouldBeCreatedWithValidData()
    {
        // Arrange
        var product = new Product
        {
            Id = 1,
            Name = "Test Product",
            Description = "Test Description",
            Price = 99.99m,
            Category = "Test Category",
            StockQuantity = 10,
            ImageUrl = "http://example.com/image.jpg",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Act & Assert
        product.Id.Should().Be(1);
        product.Name.Should().Be("Test Product");
        product.Description.Should().Be("Test Description");
        product.Price.Should().Be(99.99m);
        product.Category.Should().Be("Test Category");
        product.StockQuantity.Should().Be(10);
        product.ImageUrl.Should().Be("http://example.com/image.jpg");
        product.IsActive.Should().BeTrue();
        product.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void CreateProductRequest_ShouldValidateRequiredFields()
    {
        // Arrange
        var request = new CreateProductRequest
        {
            Name = "Valid Product",
            Price = 50.00m,
            Category = "Electronics",
            StockQuantity = 5
        };

        // Act & Assert
        request.Name.Should().NotBeNullOrEmpty();
        request.Name.Should().Be("Valid Product");
        request.Price.Should().BeGreaterThan(0);
        request.Category.Should().NotBeNullOrEmpty();
        request.StockQuantity.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void ProductResponse_ShouldHandleSuccessAndFailure()
    {
        // Arrange & Act
        var successResponse = new ProductResponse
        {
            Success = true,
            Message = "Operation successful",
            Product = ProductDto.FromEntity(new Product { Id = 1, Name = "Test" })
        };

        var failureResponse = new ProductResponse
        {
            Success = false,
            Message = "Operation failed"
        };

        // Assert
        successResponse.Success.Should().BeTrue();
        successResponse.Message.Should().Be("Operation successful");
        successResponse.Product.Should().NotBeNull();
        successResponse.Product!.Name.Should().Be("Test");

        failureResponse.Success.Should().BeFalse();
        failureResponse.Message.Should().Be("Operation failed");
        failureResponse.Product.Should().BeNull();
    }

    [Fact]
    public void UpdateStockRequest_ShouldValidateStockQuantity()
    {
        // Arrange
        var request = new UpdateStockRequest { StockQuantity = 25 };

        // Act & Assert
        request.StockQuantity.Should().BeGreaterThanOrEqualTo(0);
        request.StockQuantity.Should().Be(25);
    }
}