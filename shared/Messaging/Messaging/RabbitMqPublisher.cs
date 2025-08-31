using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Threading.Tasks;

namespace Messaging
{
    public class RabbitMqPublisher : IMessagePublisher, IDisposable
    {
        private readonly IRabbitMqConnectionManager _connectionManager;
        private readonly ILogger<RabbitMqPublisher> _logger;
        private readonly RabbitMqSettings _settings;

        public RabbitMqPublisher(
            IRabbitMqConnectionManager connectionManager,
            ILogger<RabbitMqPublisher> logger,
            IOptions<RabbitMqSettings> settings)
        {
            _connectionManager = connectionManager;
            _logger = logger;
            _settings = settings.Value;

            // Ensure exchange exists at startup using a short-lived channel
            try
            {
                using var tmpChannel = _connectionManager.GetConnectionAsync().GetAwaiter().GetResult().CreateModel();
                tmpChannel.ExchangeDeclare(
                    exchange: _settings.ExchangeName,
                    type: "direct",
                    durable: true,
                    autoDelete: false
                );

                _logger.LogInformation("RabbitMqPublisher inicializado. Exchange verificado: {ExchangeName}", _settings.ExchangeName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao inicializar RabbitMqPublisher");
                throw;
            }
        }

        public Task PublishAsync<T>(T message) where T : IEvent
        {
            // Convert EventType (PascalCase) to snake_case routing key (e.g. OrderReservationCompleted -> order_reservation_completed)
            return PublishAsync(message, ToRoutingKey(message.EventType));
        }

        private string ToRoutingKey(string eventType)
        {
            if (string.IsNullOrWhiteSpace(eventType)) return string.Empty;
            var withUnderscores = Regex.Replace(eventType, "([a-z0-9])([A-Z])", "$1_$2");
            return withUnderscores.ToLowerInvariant();
        }

        public async Task PublishAsync<T>(T message, string routingKey) where T : IEvent
        {
            // Create a short-lived channel per publish to avoid threading issues with IModel
            try
            {
                var connection = await _connectionManager.GetConnectionAsync();
                using var channel = connection.CreateModel();

                // Enable publisher confirms for this channel
                channel.ConfirmSelect();

                // Handle returned messages when mandatory=true and message not routed
                var returnReceived = false;
                void OnBasicReturn(object? sender, BasicReturnEventArgs args)
                {
                    returnReceived = true;
                    _logger.LogWarning("Message returned by broker. ReplyText: {ReplyText}, RoutingKey: {RoutingKey}", args.ReplyText, args.RoutingKey);
                }

                channel.BasicReturn += OnBasicReturn;

                var body = JsonSerializer.SerializeToUtf8Bytes(message);

                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;
                properties.Type = message.EventType;
                properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                properties.ContentType = "application/json";
                properties.MessageId = Guid.NewGuid().ToString();
                properties.CorrelationId = properties.MessageId;

                // Publish with mandatory=true so unroutable messages are returned
                channel.BasicPublish(
                    exchange: _settings.ExchangeName,
                    routingKey: routingKey,
                    mandatory: true,
                    basicProperties: properties,
                    body: body
                );

                // Wait for confirms with a timeout
                var confirmed = channel.WaitForConfirms(TimeSpan.FromSeconds(5));
                channel.BasicReturn -= OnBasicReturn;

                if (!confirmed || returnReceived)
                {
                    _logger.LogError("Falha ao confirmar publicação. EventType: {EventType}, RoutingKey: {RoutingKey}", message.EventType, routingKey);
                    throw new Exception("Message not confirmed by broker or was returned");
                }

                _logger.LogInformation(
                    "Evento publicado com sucesso. Tipo: {EventType}, RoutingKey: {RoutingKey}, Tamanho: {Size} bytes, MessageId: {MessageId}",
                    message.EventType, routingKey, body.Length, properties.MessageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao publicar evento. Tipo: {EventType}, RoutingKey: {RoutingKey}", message.EventType, routingKey);
                throw;
            }
        }

        public void Dispose()
        {
            // no-op: não mantemos canais abertos aqui
        }
    }
}
