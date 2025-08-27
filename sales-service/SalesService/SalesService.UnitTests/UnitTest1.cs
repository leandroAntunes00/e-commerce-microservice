using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace SalesService.UnitTests;

public class SalesControllerUnitTests
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
        okResult.Value.Should().Be("Sales Service is running!");
    }

    [Fact]
    public void GetSales_WithValidOrderId_ShouldReturnOk()
    {
        // Arrange
        var controller = new TestController();
        var orderId = 1;

        // Act
        var result = controller.GetSales(orderId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be($"Sales for order {orderId}: $100.00");
    }

    [Fact]
    public void GetSales_WithInvalidOrderId_ShouldReturnNotFound()
    {
        // Arrange
        var controller = new TestController();
        var invalidOrderId = -1;

        // Act
        var result = controller.GetSales(invalidOrderId);

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
        return Ok("Sales Service is running!");
    }

    [HttpGet("sales/{orderId}")]
    public IActionResult GetSales(int orderId)
    {
        if (orderId > 0)
        {
            return Ok($"Sales for order {orderId}: $100.00");
        }
        return NotFound();
    }
}