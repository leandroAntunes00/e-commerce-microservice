using System.Text.Json.Serialization;

namespace Messaging;

// Interface base para eventos
public interface IEvent
{
    string EventType { get; }
    DateTime Timestamp { get; }
}

// Classe base para eventos
public abstract class BaseEvent : IEvent
{
    [JsonIgnore]
    public abstract string EventType { get; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

// Configurações do RabbitMQ
public class RabbitMqSettings
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string ExchangeName { get; set; } = "microservices-exchange";
    public string QueuePrefix { get; set; } = "microservices.";
}

// Interface para publisher de mensagens
public interface IMessagePublisher
{
    Task PublishAsync<T>(T message) where T : IEvent;
    Task PublishAsync<T>(T message, string routingKey) where T : IEvent;
}

// Interface para consumer de mensagens
public interface IMessageConsumer
{
    Task StartConsumingAsync(string queueName, Func<string, Task> messageHandler);
    Task StopConsumingAsync();
}
