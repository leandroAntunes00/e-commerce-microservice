using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace ApiGateway.UnitTests;

public class ApiGatewayControllerUnitTests
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
        okResult.Value.Should().Be("API Gateway is running!");
    }

    [Fact]
    public void RouteRequest_WithValidPath_ShouldReturnOk()
    {
        // Arrange
        var controller = new TestController();
        var path = "/auth/health";

        // Act
        var result = controller.RouteRequest(path);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be($"Routed to {path}");
    }

    [Fact]
    public void RouteRequest_WithInvalidPath_ShouldReturnNotFound()
    {
        // Arrange
        var controller = new TestController();
        var invalidPath = "/invalid";

        // Act
        var result = controller.RouteRequest(invalidPath);

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
        return Ok("API Gateway is running!");
    }

    [HttpGet("route/{*path}")]
    public IActionResult RouteRequest(string path)
    {
        if (path.StartsWith("/auth") || path.StartsWith("/stock") || path.StartsWith("/sales") || path.StartsWith("auth") || path.StartsWith("stock") || path.StartsWith("sales"))
        {
            return Ok($"Routed to {path}");
        }
        return NotFound();
    }
}