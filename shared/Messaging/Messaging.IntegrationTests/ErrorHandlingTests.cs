using Messaging;
using Messaging.Events;
using System.Text.Json;
using Xunit;

namespace Messaging.IntegrationTests;

public class ErrorHandlingTests : IDisposable
{
    private readonly RabbitMqSettings _settings;
    private readonly IMessagePublisher _publisher;
    private readonly IMessageConsumer _consumer;

    public ErrorHandlingTests()
    {
        _settings = new RabbitMqSettings
        {
            HostName = "localhost",
            Port = 5672,
            UserName = "guest",
            Password = "guest",
            VirtualHost = "/",
            ExchangeName = "test_exchange",
            QueuePrefix = "test_"
        };

        _publisher = new RabbitMqPublisher(_settings);
        _consumer = new RabbitMqConsumer(_settings);
    }

    [Fact]
    public async Task Consumer_WithErrorHandler_ShouldMoveMessageToDLQ()
    {
        // Arrange
        var orderEvent = new OrderCreatedEvent
        {
            OrderId = 999,
            UserId = 888,
            Items = new List<OrderItemEvent>(),
            TotalAmount = 0,
            CreatedAt = DateTime.UtcNow
        };

        var dlqMessageReceived = false;
        var dlqConsumer = new RabbitMqConsumer(_settings);

        // Configurar consumidor da DLQ
        await dlqConsumer.StartConsumingAsync(
            queueName: "test_error_queue.dlq",
            messageHandler: async (message) =>
            {
                var dlqEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(message);
                if (dlqEvent?.OrderId == 999)
                {
                    dlqMessageReceived = true;
                }
            });

        // Act - Publicar mensagem que causará erro
        await _publisher.PublishAsync(orderEvent, "error_queue");

        // Consumir com handler que sempre lança erro
        await _consumer.StartConsumingAsync(
            queueName: "test_error_queue",
            messageHandler: async (message) =>
            {
                // Sempre lançar erro para testar DLQ
                throw new InvalidOperationException("Erro de teste para DLQ");
            });

        // Aguardar processamento
        await Task.Delay(3000);

        // Assert
        Assert.True(dlqMessageReceived, "Mensagem não foi movida para DLQ após erro");
    }

    [Fact]
    public async Task Publisher_WithInvalidConnection_ShouldThrowException()
    {
        // Arrange
        var invalidSettings = new RabbitMqSettings
        {
            HostName = "invalid-host",
            Port = 5672,
            UserName = "guest",
            Password = "guest",
            VirtualHost = "/",
            ExchangeName = "test_exchange",
            QueuePrefix = "test_"
        };

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () =>
        {
            var publisher = new RabbitMqPublisher(invalidSettings);
            var orderEvent = new OrderCreatedEvent
            {
                OrderId = 1,
                UserId = 1,
                Items = new List<OrderItemEvent>(),
                TotalAmount = 0,
                CreatedAt = DateTime.UtcNow
            };

            await publisher.PublishAsync(orderEvent);
        });
    }

    public void Dispose()
    {
        (_publisher as IDisposable)?.Dispose();
        (_consumer as IDisposable)?.Dispose();
    }
}
