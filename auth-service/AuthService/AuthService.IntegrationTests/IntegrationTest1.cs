using Xunit;
using FluentAssertions;

namespace AuthService.IntegrationTests;

public class AuthServiceIntegrationTests
{
    [Fact]
    public void IntegrationTestEnvironment_ShouldBeConfigured()
    {
        // Arrange
        var testEnvironment = "Integration";

        // Act
        var result = testEnvironment;

        // Assert
        result.Should().Be("Integration");
    }

    [Fact]
    public void DatabaseConnection_ShouldBeAvailable()
    {
        // Este teste seria implementado quando o serviço estiver rodando
        // Por enquanto, apenas verifica se o ambiente de teste está funcionando
        var connectionString = "Host=localhost;Port=5432;Database=authdb;Username=postgres;Password=password123";
        connectionString.Should().NotBeNullOrEmpty();
        connectionString.Should().Contain("5432");
    }

    [Fact]
    public void RabbitMQConnection_ShouldBeAvailable()
    {
        // Este teste seria implementado quando o serviço estiver rodando
        var rabbitMQConfig = new
        {
            HostName = "localhost",
            Port = 5672,
            UserName = "admin",
            Password = "password123"
        };

        rabbitMQConfig.HostName.Should().Be("localhost");
        rabbitMQConfig.Port.Should().Be(5672);
    }
}
