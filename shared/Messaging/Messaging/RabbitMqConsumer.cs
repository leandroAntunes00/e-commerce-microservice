using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Messaging
{
    public class RabbitMqConsumer : IMessageConsumer
    {
        private readonly IRabbitMqConnectionManager _connectionManager;
        private readonly ILogger<RabbitMqConsumer> _logger;
        private readonly RabbitMqSettings _settings;
        private IModel? _channel;
        private AsyncEventingBasicConsumer? _consumer;
        private string? _consumerTag;
        private Func<string, Task>? _messageHandler;

        public RabbitMqConsumer(
            IRabbitMqConnectionManager connectionManager,
            ILogger<RabbitMqConsumer> logger,
            IOptions<RabbitMqSettings> settings)
        {
            _connectionManager = connectionManager;
            _logger = logger;
            _settings = settings.Value;
        }

        public async Task StartConsumingAsync(string queueName, Func<string, Task> messageHandler)
        {
            if (_channel != null)
            {
                _logger.LogWarning("Consumer is already started.");
                return;
            }

            _messageHandler = messageHandler;

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

                _channel.QueueDeclare(
                    queue: queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false
                );

                var routingKey = queueName.Replace(_settings.QueuePrefix, "");
                _channel.QueueBind(queueName, _settings.ExchangeName, routingKey);

                _consumer = new AsyncEventingBasicConsumer(_channel);
                _consumer.Received += OnMessageReceived;

                _channel.BasicQos(0, 1, false);
                _consumerTag = _channel.BasicConsume(queueName, autoAck: false, _consumer);

                _logger.LogInformation("RabbitMqConsumer started for queue '{QueueName}'.", queueName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting RabbitMqConsumer for queue '{QueueName}'.", queueName);
                throw;
            }
            await Task.CompletedTask;
        }

        public async Task StopConsumingAsync()
        {
            if (_channel == null)
            {
                _logger.LogWarning("Consumer is not started.");
                return;
            }

            try
            {
                if (_consumerTag != null)
                {
                    _channel.BasicCancel(_consumerTag);
                }

                if (_consumer != null)
                {
                    _consumer.Received -= OnMessageReceived;
                }

                _channel?.Dispose();
                _channel = null;
                _consumer = null;
                _consumerTag = null;

                _logger.LogInformation("RabbitMqConsumer stopped.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping RabbitMqConsumer.");
            }
            await Task.CompletedTask;
        }

        private async Task OnMessageReceived(object sender, BasicDeliverEventArgs ea)
        {
            var message = Encoding.UTF8.GetString(ea.Body.ToArray());
            _logger.LogInformation("Received message from queue. DeliveryTag: {DeliveryTag}", ea.DeliveryTag);

            try
            {
                if (_messageHandler != null)
                {
                    await _messageHandler(message);
                    _channel?.BasicAck(ea.DeliveryTag, false);
                    _logger.LogInformation("Message processed successfully. DeliveryTag: {DeliveryTag}", ea.DeliveryTag);
                }
                else
                {
                    _logger.LogWarning("No message handler configured. Nacking message. DeliveryTag: {DeliveryTag}", ea.DeliveryTag);
                    _channel?.BasicNack(ea.DeliveryTag, false, false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message. Nacking and sending to DLQ. DeliveryTag: {DeliveryTag}", ea.DeliveryTag);
                _channel?.BasicNack(ea.DeliveryTag, false, false);
            }
        }

        public void Dispose()
        {
            StopConsumingAsync().Wait();
        }
    }
}
