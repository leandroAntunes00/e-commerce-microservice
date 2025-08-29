using Messaging.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Messaging.IntegrationTests
{
    public class MessagingIntegrationTests : IAsyncLifetime
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IMessagePublisher _publisher;
        private readonly IHost _host;
        private static readonly TaskCompletionSource<OrderCreatedEvent> _messageReceivedTcs = new();

        public MessagingIntegrationTests()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    {"RabbitMQ:HostName", "localhost"},
                    {"RabbitMQ:Port", "5672"},
                    {"RabbitMQ:UserName", "guest"},
                    {"RabbitMQ:Password", "guest"},
                    {"RabbitMQ:VirtualHost", "/"},
                    {"RabbitMQ:ExchangeName", "test_exchange"},
                    {"RabbitMQ:QueuePrefix", "test."}
                })
                .Build();

            var host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddLogging(builder => builder.AddConsole());
                    services.AddRabbitMqMessaging(configuration);
                    services.AddHostedService<TestOrderConsumer>();
                })
                .Build();

            _serviceProvider = host.Services;
            _publisher = _serviceProvider.GetRequiredService<IMessagePublisher>();
            _host = host;
        }

        // Consumer de teste para validar o recebimento
        private class TestOrderConsumer : QueueConsumerBackgroundService
        {
            protected override string QueueName => "test.ordercreated";

            public TestOrderConsumer(
                ILogger<TestOrderConsumer> logger,
                IRabbitMqConnectionManager connectionManager,
                IOptions<RabbitMqSettings> settings)
                : base(logger, connectionManager, settings) { }

            protected override Task<bool> HandleMessageAsync(string message, IBasicProperties properties)
            {
                var receivedEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(message);
                if (receivedEvent != null)
                {
                    _messageReceivedTcs.TrySetResult(receivedEvent);
                }
                return Task.FromResult(true);
            }
        }

        public async Task InitializeAsync()
        {
            await _host.StartAsync();
        }

        public async Task DisposeAsync()
        {
            await _host.StopAsync();
            if (_host is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else
            {
                _host.Dispose();
            }
        }

        [Fact]
        public async Task PublishAndConsume_OrderCreatedEvent_ShouldWorkCorrectly()
        {
            // Arrange
            var orderEvent = new OrderCreatedEvent
            {
                OrderId = 123,
                UserId = 456,
                Items = new List<OrderItemEvent>
                {
                    new() { ProductId = 1, ProductName = "Test Product", Quantity = 2, UnitPrice = 10.00m }
                },
                TotalAmount = 20.00m,
                CreatedAt = DateTime.UtcNow
            };

            // Act
            await _publisher.PublishAsync(orderEvent);

            // Assert
            var receivedEvent = await _messageReceivedTcs.Task.WaitAsync(TimeSpan.FromSeconds(10));

            Assert.NotNull(receivedEvent);
            Assert.Equal(orderEvent.OrderId, receivedEvent.OrderId);
            Assert.Equal(orderEvent.UserId, receivedEvent.UserId);
            Assert.Equal(orderEvent.TotalAmount, receivedEvent.TotalAmount);
            Assert.Single(receivedEvent.Items);
            Assert.Equal(orderEvent.Items[0].ProductId, receivedEvent.Items[0].ProductId);
        }
    }
}
