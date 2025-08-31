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
    public class ErrorHandlingTests : IAsyncLifetime
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IMessagePublisher _publisher;
        private readonly IHost _host;
        private static readonly TaskCompletionSource<OrderCreatedEvent> _dlqMessageReceivedTcs = new();

        public ErrorHandlingTests()
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
                    services.AddHostedService<ErroringConsumer>();
                    services.AddHostedService<DlqConsumer>();
                })
                .Build();

            _serviceProvider = host.Services;
            _publisher = _serviceProvider.GetRequiredService<IMessagePublisher>();
            _host = host;
        }

        // Consumidor que sempre falha, para forçar a mensagem para a DLQ
        private class ErroringConsumer : QueueConsumerBackgroundService
        {
            protected override string QueueName => "test.errorqueue";

            public ErroringConsumer(ILogger<ErroringConsumer> logger, IRabbitMqConnectionManager connectionManager, IOptions<RabbitMqSettings> settings)
                : base(logger, connectionManager, settings) { }

            protected override Task<bool> HandleMessageAsync(string message, IBasicProperties properties)
            {
                // Sempre falha para testar a DLQ
                return Task.FromResult(false);
            }
        }

        // Consumidor para a DLQ
        private class DlqConsumer : QueueConsumerBackgroundService
        {
            protected override string QueueName => "test.errorqueue.dlq";

            public DlqConsumer(ILogger<DlqConsumer> logger, IRabbitMqConnectionManager connectionManager, IOptions<RabbitMqSettings> settings)
                : base(logger, connectionManager, settings) { }

            protected override Task<bool> HandleMessageAsync(string message, IBasicProperties properties)
            {
                var receivedEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(message);
                if (receivedEvent != null)
                {
                    _dlqMessageReceivedTcs.TrySetResult(receivedEvent);
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
        public async Task Consumer_WithProcessingError_ShouldMoveMessageToDlq()
        {
            // Arrange
            var orderEvent = new OrderCreatedEvent
            {
                OrderId = 999,
                UserId = 888,
                CreatedAt = DateTime.UtcNow
            };

            // Act
            // Publica na fila que o ErroringConsumer está escutando
            await _publisher.PublishAsync(orderEvent, "errorqueue");

            // Assert
            // Aguarda o DlqConsumer receber a mensagem
            var receivedEvent = await _dlqMessageReceivedTcs.Task.WaitAsync(TimeSpan.FromSeconds(10));

            Assert.NotNull(receivedEvent);
            Assert.Equal(orderEvent.OrderId, receivedEvent.OrderId);
        }
    }
}
