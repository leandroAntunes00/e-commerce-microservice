using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Messaging
{
    public abstract class QueueConsumerBackgroundService : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly IRabbitMqConnectionManager _connectionManager;
        private readonly RabbitMqSettings _settings;
        private IModel? _channel;

        protected abstract string QueueName { get; }
        protected abstract Task<bool> HandleMessageAsync(string message, IBasicProperties properties);

        protected QueueConsumerBackgroundService(
            ILogger logger,
            IRabbitMqConnectionManager connectionManager,
            IOptions<RabbitMqSettings> settings)
        {
            _logger = logger;
            _connectionManager = connectionManager;
            _settings = settings.Value;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        { 
            _logger.LogInformation("Starting consumer for queue '{QueueName}'.", QueueName);
            
            var connection = _connectionManager.GetConnection();
            _channel = connection.CreateModel();

            _channel.ExchangeDeclare(_settings.ExchangeName, "direct", durable: true, autoDelete: false);
            
            // Configurar DLQ apenas se não for uma DLQ
            var isDlq = QueueName.EndsWith(".dlq");
            if (!isDlq)
            {
                var queueArgs = new Dictionary<string, object>
                {
                    { "x-dead-letter-exchange", "" }, // Default exchange
                    { "x-dead-letter-routing-key", $"{QueueName}.dlq" }
                };

                _channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false, arguments: queueArgs);
                
                // Declarar a DLQ
                _channel.QueueDeclare($"{QueueName}.dlq", durable: true, exclusive: false, autoDelete: false, arguments: null);
            }
            else
            {
                _channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
            }
            
            var routingKey = QueueName.Replace(_settings.QueuePrefix, "");
            _channel.QueueBind(QueueName, _settings.ExchangeName, routingKey);

            return base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var consumer = new AsyncEventingBasicConsumer(_channel!);
            consumer.Received += OnMessageReceived;

            _channel!.BasicQos(0, 1, false);
            _channel.BasicConsume(QueueName, autoAck: false, consumer);

            _logger.LogInformation("Consumer started for queue '{QueueName}'. Waiting for messages.", QueueName);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async Task OnMessageReceived(object sender, BasicDeliverEventArgs ea)
        {
            var message = Encoding.UTF8.GetString(ea.Body.ToArray());
            _logger.LogInformation("Received message from queue '{QueueName}'. DeliveryTag: {DeliveryTag}", QueueName, ea.DeliveryTag);

            try
            {
                var success = await HandleMessageAsync(message, ea.BasicProperties);
                if (success)
                {
                    _channel!.BasicAck(ea.DeliveryTag, false);
                    _logger.LogInformation("Message processed successfully. DeliveryTag: {DeliveryTag}", ea.DeliveryTag);
                }
                else
                {
                    _logger.LogWarning("Failed to process message. Nacking and sending to DLQ. DeliveryTag: {DeliveryTag}", ea.DeliveryTag);
                    _channel!.BasicNack(ea.DeliveryTag, false, false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message. Nacking and sending to DLQ. DeliveryTag: {DeliveryTag}", ea.DeliveryTag);
                _channel!.BasicNack(ea.DeliveryTag, false, false);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        { 
            _logger.LogInformation("Stopping consumer for queue '{QueueName}'.", QueueName);
            await base.StopAsync(cancellationToken);
            _channel?.Dispose();
        }
    }
}
