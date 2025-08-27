using Messaging;
using Messaging.Events;
using System.Text.Json;
using Xunit;
using Microsoft.Extensions.Configuration;

namespace Messaging.IntegrationTests;

public class MessagingIntegrationTests : IDisposable
{
    private readonly RabbitMqSettings _settings;
    private readonly IMessagePublisher _publisher;
    private readonly IMessageConsumer _consumer;
    private bool _messageReceived;
    private OrderCreatedEvent? _receivedEvent;

    public MessagingIntegrationTests()
    {
        // Configurações para testes - usar RabbitMQ local
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
    public async Task PublishAndConsume_OrderCreatedEvent_ShouldWorkCorrectly()
    {
        // Arrange
        var orderEvent = new OrderCreatedEvent
        {
            OrderId = 123,
            UserId = 456,
            Items = new List<OrderItemEvent>
            {
                new OrderItemEvent
                {
                    ProductId = 1,
                    ProductName = "Test Product",
                    Quantity = 2,
                    UnitPrice = 10.00m
                }
            },
            TotalAmount = 20.00m,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await _publisher.PublishAsync(orderEvent);

        // Assert - verificar se a mensagem foi publicada
        await Task.Delay(1000); // Aguardar um pouco

        // Consumir a mensagem
        await _consumer.StartConsumingAsync(
            queueName: $"{_settings.QueuePrefix}order_created",
            messageHandler: async (message) =>
            {
                _receivedEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(message);
                _messageReceived = true;
            });

        // Aguardar até receber a mensagem ou timeout
        var timeout = DateTime.UtcNow.AddSeconds(10);
        while (!_messageReceived && DateTime.UtcNow < timeout)
        {
            await Task.Delay(100);
        }

        // Assert
        Assert.True(_messageReceived, "Mensagem não foi recebida dentro do timeout");
        Assert.NotNull(_receivedEvent);
        Assert.Equal(orderEvent.OrderId, _receivedEvent.OrderId);
        Assert.Equal(orderEvent.UserId, _receivedEvent.UserId);
        Assert.Equal(orderEvent.TotalAmount, _receivedEvent.TotalAmount);
        Assert.Single(_receivedEvent.Items);
        Assert.Equal(orderEvent.Items[0].ProductId, _receivedEvent.Items[0].ProductId);
    }

    [Fact]
    public async Task Publish_OrderCreatedEvent_WithCustomRoutingKey_ShouldWork()
    {
        // Arrange
        var orderEvent = new OrderCreatedEvent
        {
            OrderId = 789,
            UserId = 101,
            Items = new List<OrderItemEvent>(),
            TotalAmount = 0,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await _publisher.PublishAsync(orderEvent, "custom_routing_key");

        // Assert
        await Task.Delay(500); // Aguardar processamento

        // Verificar se conseguimos consumir com a routing key customizada
        var messageReceived = false;
        await _consumer.StartConsumingAsync(
            queueName: "test_custom_queue",
            messageHandler: async (message) =>
            {
                var receivedEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(message);
                if (receivedEvent?.OrderId == 789)
                {
                    messageReceived = true;
                }
            });

        // Aguardar
        await Task.Delay(2000);

        Assert.True(messageReceived, "Mensagem com routing key customizada não foi recebida");
    }

    [Fact]
    public async Task MultipleEvents_ShouldBeProcessedInOrder()
    {
        // Arrange
        var events = new List<OrderCreatedEvent>();
        var receivedEvents = new List<OrderCreatedEvent>();
        var messageCount = 0;

        for (int i = 1; i <= 5; i++)
        {
            events.Add(new OrderCreatedEvent
            {
                OrderId = i,
                UserId = 100 + i,
                Items = new List<OrderItemEvent>(),
                TotalAmount = i * 10.0m,
                CreatedAt = DateTime.UtcNow
            });
        }

        // Act - Publicar todos os eventos
        foreach (var orderEvent in events)
        {
            await _publisher.PublishAsync(orderEvent);
        }

        // Consumir eventos
        await _consumer.StartConsumingAsync(
            queueName: $"{_settings.QueuePrefix}order_created_batch",
            messageHandler: async (message) =>
            {
                var receivedEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(message);
                if (receivedEvent != null)
                {
                    receivedEvents.Add(receivedEvent);
                    messageCount++;
                }
            });

        // Aguardar até receber todas as mensagens
        var timeout = DateTime.UtcNow.AddSeconds(15);
        while (messageCount < 5 && DateTime.UtcNow < timeout)
        {
            await Task.Delay(200);
        }

        // Assert
        Assert.Equal(5, messageCount);
        Assert.Equal(5, receivedEvents.Count);

        // Verificar se os eventos foram recebidos na ordem correta
        for (int i = 0; i < receivedEvents.Count; i++)
        {
            Assert.Equal(i + 1, receivedEvents[i].OrderId);
        }
    }

    public void Dispose()
    {
        (_publisher as IDisposable)?.Dispose();
        (_consumer as IDisposable)?.Dispose();
    }
}
