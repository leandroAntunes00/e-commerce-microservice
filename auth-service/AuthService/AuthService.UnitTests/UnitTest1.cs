using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace AuthService.UnitTests;

public class AuthControllerUnitTests
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
        okResult.Value.Should().Be("Auth Service is running!");
    }

    [Fact]
    public void ValidateToken_WithValidToken_ShouldReturnOk()
    {
        // Arrange
        var controller = new TestController();
        var validToken = "valid.jwt.token";

        // Act
        var result = controller.ValidateToken(validToken);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be("Token is valid");
    }

    [Fact]
    public void ValidateToken_WithInvalidToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var controller = new TestController();
        var invalidToken = "invalid.token";

        // Act
        var result = controller.ValidateToken(invalidToken);

        // Assert
        result.Should().BeOfType<UnauthorizedResult>();
    }
}

// Controller de teste simples para demonstrar os testes unitários
public class TestController : ControllerBase
{
    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok("Auth Service is running!");
    }

    [HttpPost("validate")]
    public IActionResult ValidateToken(string token)
    {
        if (token == "valid.jwt.token")
        {
            return Ok("Token is valid");
        }
        return Unauthorized();
    }
}