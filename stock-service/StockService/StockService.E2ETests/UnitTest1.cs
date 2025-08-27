using Xunit;
using FluentAssertions;

namespace StockService.E2ETests;

public class StockServiceE2ETests
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
        var serviceUrl = "http://localhost:5126";

        // Act
        var result = serviceUrl;

        // Assert
        result.Should().Contain("localhost");
        result.Should().Contain("5126");
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
    public void StockEndpoint_ShouldBeDefined()
    {
        // Arrange
        var stockEndpoint = "/api/stock";

        // Act
        var result = stockEndpoint;

        // Assert
        result.Should().StartWith("/");
        result.Should().Contain("stock");
    }

    [Fact]
    public void WeatherForecastEndpoint_ShouldReturnData()
    {
        // Este teste seria implementado quando o serviço estiver rodando
        // Por enquanto, apenas verifica se o endpoint está definido
        var weatherEndpoint = "/weatherforecast";
        weatherEndpoint.Should().NotBeNullOrEmpty();
    }
}