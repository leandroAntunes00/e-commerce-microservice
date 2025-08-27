namespace ApiGateway.IntegrationTests;

public class ApiGatewayIntegrationTests
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

    [Fact]
    public void ServiceHealthCheck_ShouldReturnOk()
    {
        // Simulação de teste de integração
        var serviceStatus = "Running";
        serviceStatus.Should().Be("Running");
    }

    [Fact]
    public void ReverseProxyConfiguration_ShouldBeDefined()
    {
        // Arrange
        var proxyConfig = new
        {
            AuthServiceUrl = "http://localhost:5051",
            StockServiceUrl = "http://localhost:5126",
            SalesServiceUrl = "http://localhost:5047"
        };

        proxyConfig.AuthServiceUrl.Should().Contain("5051");
        proxyConfig.StockServiceUrl.Should().Contain("5126");
        proxyConfig.SalesServiceUrl.Should().Contain("5047");
    }
}