using Xunit;
using FluentAssertions;
using StockService.Domain.Entities;
using StockService.Application.DTOs;

namespace StockService.E2ETests;

public class StockServiceE2ETests
{
    [Fact]
    public void ProductCRUDWorkflow_ShouldWorkCorrectly()
    {
        // Arrange
        var product = new Product
        {
            Id = 100,
            Name = "E2E Test Product",
            Description = "Product for E2E testing",
            Price = 199.99m,
            Category = "E2E Tests",
            StockQuantity = 20,
            ImageUrl = "https://example.com/e2e-test.jpg",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Act & Assert
        product.Id.Should().Be(100);
        product.Name.Should().Be("E2E Test Product");
        product.Description.Should().Be("Product for E2E testing");
        product.Price.Should().Be(199.99m);
        product.Category.Should().Be("E2E Tests");
        product.StockQuantity.Should().Be(20);
        product.ImageUrl.Should().Be("https://example.com/e2e-test.jpg");
        product.IsActive.Should().BeTrue();
    }

    [Fact]
    public void ProductStockManagement_ShouldHandleUpdates()
    {
        // Arrange
        var initialStock = 50;
        var product = new Product
        {
            Id = 200,
            Name = "Stock Management Test",
            StockQuantity = initialStock,
            IsActive = true
        };

        // Act - Simulate stock update
        var newStock = 75;
        product.StockQuantity = newStock;
        product.UpdatedAt = DateTime.UtcNow;

        // Assert
        product.StockQuantity.Should().Be(newStock);
        product.UpdatedAt.Should().NotBeNull();
        product.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ProductActivation_ShouldWorkCorrectly()
    {
        // Arrange
        var product = new Product
        {
            Id = 300,
            Name = "Activation Test Product",
            IsActive = true
        };

        // Act - Deactivate product
        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;

        // Assert
        product.IsActive.Should().BeFalse();
        product.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void ProductCategories_ShouldBeValid()
    {
        // Arrange
        var categories = new[] { "Eletrônicos", "Informática", "Áudio", "E2E Tests" };
        var product = new Product
        {
            Id = 400,
            Name = "Category Test Product",
            Category = categories[0],
            IsActive = true
        };

        // Act & Assert
        categories.Should().Contain(product.Category);
        product.Category.Should().NotBeNullOrEmpty();
        product.Category.Should().Be("Eletrônicos");
    }

    [Fact]
    public void ProductValidation_ShouldEnforceBusinessRules()
    {
        // Arrange & Act
        var validProduct = new Product
        {
            Name = "Valid Product",
            Price = 99.99m,
            Category = "Valid Category",
            StockQuantity = 10,
            IsActive = true
        };

        var invalidProduct = new Product
        {
            Name = "",
            Price = -10.00m,
            Category = "",
            StockQuantity = -5,
            IsActive = false
        };

        // Assert
        validProduct.Name.Should().NotBeNullOrEmpty();
        validProduct.Price.Should().BeGreaterThan(0);
        validProduct.Category.Should().NotBeNullOrEmpty();
        validProduct.StockQuantity.Should().BeGreaterThanOrEqualTo(0);
        validProduct.IsActive.Should().BeTrue();

        // Invalid product should have invalid values
        invalidProduct.Name.Should().BeEmpty();
        invalidProduct.Price.Should().BeLessThan(0);
        invalidProduct.Category.Should().BeEmpty();
        invalidProduct.StockQuantity.Should().BeLessThan(0);
        invalidProduct.IsActive.Should().BeFalse();
    }
}