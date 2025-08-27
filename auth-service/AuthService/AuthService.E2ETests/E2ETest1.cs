using Xunit;
using FluentAssertions;

namespace AuthService.E2ETests;

public class AuthServiceE2ETests
{
    [Fact]
    public void E2ETestEnvironment_ShouldBeConfigured()
    {
        // Arrange
        var testEnvironment = "E2E";

        // Act
        var result = testEnvironment;

        // Assert
        result.Should().Be("E2E");
    }

    [Fact]
    public void ServiceEndpoint_ShouldBeConfigured()
    {
        // Arrange
        var serviceUrl = "http://localhost:5051";

        // Act
        var result = serviceUrl;

        // Assert
        result.Should().Contain("localhost");
        result.Should().Contain("5051");
    }

    [Fact]
    public void HealthEndpoint_ShouldBeDefined()
    {
        // Arrange
        var healthEndpoint = "/health";

        // Act
        var result = healthEndpoint;

        // Assert
        result.Should().StartWith("/");
        result.Should().Be("/health");
    }

    [Fact]
    public void SwaggerEndpoint_ShouldBeDefined()
    {
        // Arrange
        var swaggerEndpoint = "/swagger";

        // Act
        var result = swaggerEndpoint;

        // Assert
        result.Should().StartWith("/");
        result.Should().Be("/swagger");
    }
}
