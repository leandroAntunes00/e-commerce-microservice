using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Messaging;

public class RabbitMqConsumer : IMessageConsumer, IDisposable
{
    private readonly object _connection;
    private readonly object _channel;
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqConsumer> _logger;
    private string? _consumerTag;

    public RabbitMqConsumer(RabbitMqSettings settings, ILogger<RabbitMqConsumer> logger)
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

        _logger.LogInformation("RabbitMqConsumer inicializado com sucesso. Exchange: {ExchangeName}", _settings.ExchangeName);
    }

    public async Task StartConsumingAsync(string queueName, Func<string, Task> messageHandler)
    {
        await Task.Run(() =>
        {
            // Declarar fila
            _channel.GetType().GetMethod("QueueDeclare")!.Invoke(_channel, new object[]
            {
                queueName,
                true,
                false,
                false
            });

            // Vincular fila ao exchange
            _channel.GetType().GetMethod("QueueBind")!.Invoke(_channel, new object[]
            {
                queueName,
                _settings.ExchangeName,
                queueName.Replace(_settings.QueuePrefix, "")
            });

            var consumerType = Type.GetType("RabbitMQ.Client.EventingBasicConsumer, RabbitMQ.Client");
            if (consumerType == null)
            {
                throw new InvalidOperationException("RabbitMQ.Client.EventingBasicConsumer type not found. Ensure RabbitMQ.Client is referenced.");
            }

            var consumer = Activator.CreateInstance(consumerType, new object[] { _channel }) ?? throw new InvalidOperationException("Failed to create EventingBasicConsumer instance.");

            // Configurar o evento Received
            var receivedEvent = consumerType.GetEvent("Received") ?? throw new InvalidOperationException("Event 'Received' not found on EventingBasicConsumer.");
            var handlerType = receivedEvent.EventHandlerType;
            var handler = Delegate.CreateDelegate(handlerType!, new MessageHandlerWrapper(queueName, messageHandler, _logger), "HandleMessage");

            receivedEvent.AddEventHandler(consumer, handler);

            // Configurar QoS para processar uma mensagem por vez
            _channel.GetType().GetMethod("BasicQos")!.Invoke(_channel, new object[] { 0, 1, false });

            var consumeResult = _channel.GetType().GetMethod("BasicConsume")!.Invoke(_channel, new object[]
            {
                queueName,
                false, // autoAck
                consumer
            });

            _consumerTag = consumeResult as string;
        });
    }

    public async Task StopConsumingAsync()
    {
        await Task.Run(() =>
        {
            if (!string.IsNullOrEmpty(_consumerTag))
            {
                _channel.GetType().GetMethod("BasicCancel")!.Invoke(_channel, new object[] { _consumerTag });
                _consumerTag = null;
            }
        });
    }

    public void Dispose()
    {
        StopConsumingAsync().Wait();
        (_channel as IDisposable)?.Dispose();
        (_connection as IDisposable)?.Dispose();
    }
}

public class MessageHandlerWrapper
{
    private readonly string _queueName;
    private readonly Func<string, Task> _messageHandler;
    private readonly ILogger<RabbitMqConsumer> _logger;

    public MessageHandlerWrapper(string queueName, Func<string, Task> messageHandler, ILogger<RabbitMqConsumer> logger)
    {
        _queueName = queueName;
        _messageHandler = messageHandler;
        _logger = logger;
    }

    public void HandleMessage(object sender, object eventArgs)
    {
        try
        {
            var body = (byte[])eventArgs.GetType().GetProperty("Body")!.GetValue(eventArgs)!;
            var message = Encoding.UTF8.GetString(body);
            var deliveryTag = (ulong)eventArgs.GetType().GetProperty("DeliveryTag")!.GetValue(eventArgs)!;

            _logger.LogInformation("Mensagem recebida. DeliveryTag: {DeliveryTag}, Tamanho: {Size} bytes", deliveryTag, body.Length);

            // Criar um handler que processa a mensagem como string
            Task.Run(async () =>
            {
                try
                {
                    await _messageHandler(message);

                    // Confirmar processamento da mensagem
                    var channel = sender.GetType().GetProperty("Model")!.GetValue(sender);
                    channel!.GetType().GetMethod("BasicAck")!.Invoke(channel, new object[] { deliveryTag, false });
                    _logger.LogInformation("Mensagem processada com sucesso. DeliveryTag: {DeliveryTag}", deliveryTag);
                }
                catch (Exception ex)
                {
                    // Em caso de erro, rejeitar a mensagem e enviar para DLQ
                    var channel = sender.GetType().GetProperty("Model")!.GetValue(sender);
                    var dlqName = $"{_queueName}.dlq";
                    channel!.GetType().GetMethod("QueueDeclare")!.Invoke(channel, new object[]
                    {
                        dlqName,
                        true,
                        false,
                        false
                    });

                    // Publicar na DLQ
                    var properties = channel.GetType().GetMethod("CreateBasicProperties")!.Invoke(channel, null);
                    properties!.GetType().GetProperty("Persistent")!.SetValue(properties, true);
                    properties.GetType().GetProperty("Headers")!.SetValue(properties, new Dictionary<string, object>
                    {
                        ["x-original-queue"] = _queueName,
                        ["x-error-message"] = ex.Message,
                        ["x-error-timestamp"] = DateTime.UtcNow.ToString("O")
                    });

                    channel.GetType().GetMethod("BasicPublish")!.Invoke(channel, new object[]
                    {
                        "",
                        dlqName,
                        properties,
                        body
                    });

                    // Rejeitar mensagem original
                    channel.GetType().GetMethod("BasicNack")!.Invoke(channel, new object[] { deliveryTag, false, false });
                    _logger.LogError(ex, "Mensagem movida para DLQ. DeliveryTag: {DeliveryTag}, DLQ: {DlqName}", deliveryTag, dlqName);
                }
            }).Wait();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro crítico ao processar mensagem");
        }
    }
}
