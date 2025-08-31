using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using FluentAssertions;
using Moq;
using SalesService.Api.Dtos;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Collections.Generic;
using System.Text.Json;

namespace SalesService.UnitTests;

// Simple auth handler to bypass authentication in tests
#pragma warning disable 0618
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, System.Text.Encodings.Web.UrlEncoder encoder, ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "1") };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
#pragma warning restore 0618

public class TestWebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });

            // Replace SalesDbContext with in-memory database for tests
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(Microsoft.EntityFrameworkCore.DbContextOptions<SalesService.Data.SalesDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<SalesService.Data.SalesDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDb");
            });

            // Remove hosted services that may try to connect to external systems (RabbitMQ) during tests
            var hostedDescriptors = services.Where(d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService)).ToList();
            foreach (var hd in hostedDescriptors)
            {
                // remove only ReservationResultConsumerService if present
                if (hd.ImplementationType != null && hd.ImplementationType.FullName != null && hd.ImplementationType.FullName.Contains("ReservationResultConsumerService"))
                {
                    services.Remove(hd);
                }
            }

            // Replace IMessagePublisher with a mocked implementation to avoid RabbitMQ initialization
            var pubDesc = services.SingleOrDefault(d => d.ServiceType != null && d.ServiceType.FullName != null && d.ServiceType.FullName.Contains("IMessagePublisher"));
            if (pubDesc != null)
            {
                services.Remove(pubDesc);
            }

            var mockPublisher = new Moq.Mock<Messaging.IMessagePublisher>();
            services.AddSingleton(mockPublisher.Object);

            // Mock IStockServiceClient to return a valid product for happy path tests
            var stockMock = new Moq.Mock<SalesService.Services.IStockServiceClient>();
            stockMock.Setup(s => s.GetProductAsync(It.IsAny<int>())).ReturnsAsync(new SalesService.Services.StockProductResponse
            {
                Id = 1,
                Name = "Prod",
                Price = 10m,
                StockQuantity = 100
            });
            services.AddSingleton(stockMock.Object);
        });
    }

}

public class SalesControllerIntegrationTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;

    public SalesControllerIntegrationTests(TestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateOrder_WithValidItems_ReturnsCreated()
    {
        // Arrange
        var client = _factory.CreateClient();

        var request = new CreateOrderRequest
        {
            Items = new List<OrderItemRequest>
            {
                new OrderItemRequest { ProductId = 1, Quantity = 2 }
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/sales/orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        // ASP.NET Core serializes to camelCase by default
        body.TryGetProperty("orderId", out var orderIdProp).Should().BeTrue();
        orderIdProp.GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateOrder_WithEmptyItems_ReturnsValidationError()
    {
        // Arrange
        var client = _factory.CreateClient();

        var request = new CreateOrderRequest { Items = new List<OrderItemRequest>() };

        // Act
        var response = await client.PostAsJsonAsync("/api/sales/orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("Validation failed");
        json.Should().Contain("At least one item is required");
    }
}
