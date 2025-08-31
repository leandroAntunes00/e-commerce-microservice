using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Moq;
using SalesService.Services;

namespace SalesService.UnitTests.Services;

public class StockServiceClientTests
{
    private static IHttpClientFactory CreateFactory(HttpMessageHandler handler)
    {
        var client = new HttpClient(handler)
        {
            BaseAddress = new System.Uri("http://stock")
        };
        var mock = new Mock<IHttpClientFactory>();
        mock.Setup(f => f.CreateClient("StockService")).Returns(client);
        return mock.Object;
    }

    [Fact]
    public async Task CheckStockAvailability_ReturnsTrue_WhenSufficientStock()
    {
        var envelope = new StockServiceEnvelope { Success = true, Product = new StockProductResponse { Id = 1, StockQuantity = 10 } };
        var handler = new TestHttpMessageHandler(JsonContent.Create(envelope), HttpStatusCode.OK);
        var factory = CreateFactory(handler);

        var client = new StockServiceClient(factory);
        var ok = await client.CheckStockAvailability(1, 5);
        ok.Should().BeTrue();
    }

    [Fact]
    public async Task CheckStockAvailability_ReturnsFalse_OnNonSuccess()
    {
        var handler = new TestHttpMessageHandler(new StringContent("err"), HttpStatusCode.InternalServerError);
        var factory = CreateFactory(handler);

        var client = new StockServiceClient(factory);
        var ok = await client.CheckStockAvailability(1, 5);
        ok.Should().BeFalse();
    }

    [Fact]
    public async Task GetProductAsync_ReturnsProduct_WhenOk()
    {
        var envelope = new StockServiceEnvelope { Success = true, Product = new StockProductResponse { Id = 2, Name = "P", StockQuantity = 3 } };
        var handler = new TestHttpMessageHandler(JsonContent.Create(envelope), HttpStatusCode.OK);
        var factory = CreateFactory(handler);

        var client = new StockServiceClient(factory);
        var p = await client.GetProductAsync(2);
        p.Should().NotBeNull();
        p!.Id.Should().Be(2);
    }

    [Fact]
    public async Task ReserveStock_ReturnsTrue_WhenEnvelopeSuccess()
    {
        var envelope = new StockServiceEnvelope { Success = true };
        var handler = new TestHttpMessageHandler(JsonContent.Create(envelope), HttpStatusCode.OK);
        var factory = CreateFactory(handler);

        var client = new StockServiceClient(factory);
        var ok = await client.ReserveStock(5, 2);
        ok.Should().BeTrue();
    }

    [Fact]
    public async Task ReleaseStock_ReturnsFalse_WhenNonSuccessStatus()
    {
        var handler = new TestHttpMessageHandler(new StringContent("err"), HttpStatusCode.InternalServerError);
        var factory = CreateFactory(handler);

        var client = new StockServiceClient(factory);
        var ok = await client.ReleaseStock(5, 1);
        ok.Should().BeFalse();
    }

    // simple handler that returns provided response
    private class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpContent _content;
        private readonly HttpStatusCode _status;
        public TestHttpMessageHandler(HttpContent content, HttpStatusCode status)
        {
            _content = content;
            _status = status;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            var resp = new HttpResponseMessage(_status)
            {
                Content = _content
            };
            return Task.FromResult(resp);
        }
    }
}
