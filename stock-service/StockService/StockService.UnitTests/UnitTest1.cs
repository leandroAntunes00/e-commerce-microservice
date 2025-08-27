using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace StockService.UnitTests;

public class StockControllerUnitTests
{
    [Fact]
    public void HealthCheck_ShouldReturnOk()
    {
        // Arrange
        var controller = new TestController();

        // Act
        var result = controller.HealthCheck();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be("Stock Service is running!");
    }

    [Fact]
    public void GetStock_WithValidProductId_ShouldReturnOk()
    {
        // Arrange
        var controller = new TestController();
        var productId = 1;

        // Act
        var result = controller.GetStock(productId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be($"Stock for product {productId}: 100 units");
    }

    [Fact]
    public void GetStock_WithInvalidProductId_ShouldReturnNotFound()
    {
        // Arrange
        var controller = new TestController();
        var invalidProductId = -1;

        // Act
        var result = controller.GetStock(invalidProductId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }
}

// Controller de teste simples para demonstrar os testes unitários
public class TestController : ControllerBase
{
    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok("Stock Service is running!");
    }

    [HttpGet("stock/{productId}")]
    public IActionResult GetStock(int productId)
    {
        if (productId > 0)
        {
            return Ok($"Stock for product {productId}: 100 units");
        }
        return NotFound();
    }
}