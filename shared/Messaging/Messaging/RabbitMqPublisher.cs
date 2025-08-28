using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System;
using System.Threading.Tasks;

namespace Messaging
{
    public class RabbitMqPublisher : IMessagePublisher, IDisposable
    {
        private readonly IRabbitMqConnectionManager _connectionManager;
        private readonly ILogger<RabbitMqPublisher> _logger;
        private readonly RabbitMqSettings _settings;
        private readonly IModel _channel;


  
        public RabbitMqPublisher(
            IRabbitMqConnectionManager connectionManager,
            ILogger<RabbitMqPublisher> logger,
            IOptions<RabbitMqSettings> settings)
        {
            _connectionManager = connectionManager;
            _logger = logger;
            _settings = settings.Value;

            try
            {
                var connection = _connectionManager.GetConnection();
                _channel = connection.CreateModel();

                _channel.ExchangeDeclare(
                    exchange: _settings.ExchangeName,
                    type: "direct",
                    durable: true,
                    autoDelete: false
                );

                _logger.LogInformation("RabbitMqPublisher inicializado com sucesso. Exchange: {ExchangeName}", _settings.ExchangeName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao inicializar RabbitMqPublisher");
                throw;
            }
        }

        public Task PublishAsync<T>(T message) where T : IEvent
        {
            return PublishAsync(message, message.EventType.ToLowerInvariant());
        }

        public Task PublishAsync<T>(T message, string routingKey) where T : IEvent
        {
            try
            {
                var body = JsonSerializer.SerializeToUtf8Bytes(message);

                var properties = _channel.CreateBasicProperties();
                properties.Persistent = true;
                properties.Type = message.EventType;
                properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                properties.ContentType = "application/json";

                _channel.BasicPublish(
                    exchange: _settings.ExchangeName,
                    routingKey: routingKey,
                    basicProperties: properties,
                    body: body
                );

                _logger.LogInformation(
                    "Evento publicado com sucesso. Tipo: {EventType}, RoutingKey: {RoutingKey}, Tamanho: {Size} bytes",
                    message.EventType, routingKey, body.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao publicar evento. Tipo: {EventType}, RoutingKey: {RoutingKey}",
                    message.EventType, routingKey);
                throw;
            }

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _channel?.Dispose();
        }
    }
}
