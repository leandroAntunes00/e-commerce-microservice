using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Messaging;

public class RabbitMqPublisher : IMessagePublisher, IDisposable
{
    private readonly object _connection;
    private readonly object _channel;
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqPublisher> _logger;

    public RabbitMqPublisher(RabbitMqSettings settings, ILogger<RabbitMqPublisher> logger)
    {
        _settings = settings;
        _logger = logger;

        // Usar reflexão para criar os objetos RabbitMQ dinamicamente
        var connectionFactoryType = Type.GetType("RabbitMQ.Client.ConnectionFactory, RabbitMQ.Client");
        var factory = Activator.CreateInstance(connectionFactoryType!);

        // Configurar propriedades
        connectionFactoryType!.GetProperty("HostName")!.SetValue(factory, _settings.HostName);
        connectionFactoryType.GetProperty("Port")!.SetValue(factory, _settings.Port);
        connectionFactoryType.GetProperty("UserName")!.SetValue(factory, _settings.UserName);
        connectionFactoryType.GetProperty("Password")!.SetValue(factory, _settings.Password);
        connectionFactoryType.GetProperty("VirtualHost")!.SetValue(factory, _settings.VirtualHost);

        // Criar conexão e canal
        _connection = connectionFactoryType.GetMethod("CreateConnection")!.Invoke(factory, null)!;
        _channel = _connection.GetType().GetMethod("CreateModel")!.Invoke(_connection, null)!;

        // Declarar exchange
        _channel.GetType().GetMethod("ExchangeDeclare")!.Invoke(_channel, new object[]
        {
            _settings.ExchangeName,
            "direct",
            true,
            false
        });

        _logger.LogInformation("RabbitMqPublisher inicializado com sucesso. Exchange: {ExchangeName}", _settings.ExchangeName);
    }

    public async Task PublishAsync<T>(T message) where T : IEvent
    {
        await PublishAsync(message, message.EventType.ToLower());
    }

    public async Task PublishAsync<T>(T message, string routingKey) where T : IEvent
    {
        await Task.Run(() =>
        {
            try
            {
                var messageJson = JsonSerializer.Serialize(message);
                var body = Encoding.UTF8.GetBytes(messageJson);

                var properties = _channel.GetType().GetMethod("CreateBasicProperties")!.Invoke(_channel, null);
                properties!.GetType().GetProperty("Persistent")!.SetValue(properties, true);
                properties.GetType().GetProperty("Type")!.SetValue(properties, message.EventType);
                properties.GetType().GetProperty("Timestamp")!.SetValue(properties, DateTime.UtcNow);

                _channel.GetType().GetMethod("BasicPublish")!.Invoke(_channel, new object[]
                {
                    _settings.ExchangeName,
                    routingKey,
                    properties,
                    body
                });

                _logger.LogInformation("Evento publicado com sucesso. Tipo: {EventType}, RoutingKey: {RoutingKey}, Tamanho: {Size} bytes",
                    message.EventType, routingKey, body.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao publicar evento. Tipo: {EventType}, RoutingKey: {RoutingKey}",
                    message.EventType, routingKey);
                throw;
            }
        });
    }

    public void Dispose()
    {
        (_channel as IDisposable)?.Dispose();
        (_connection as IDisposable)?.Dispose();
    }
}
