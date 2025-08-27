using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using System.Reflection;

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
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Carregar assembly RabbitMQ.Client dinamicamente
        Assembly? rabbitAssembly = null;
        try
        {
            rabbitAssembly = Assembly.Load("RabbitMQ.Client");
        }
        catch { }

        if (rabbitAssembly == null)
        {
            throw new InvalidOperationException("RabbitMQ.Client assembly not found. Ensure RabbitMQ.Client package is available at runtime.");
        }

        var connectionFactoryType = rabbitAssembly.GetType("RabbitMQ.Client.ConnectionFactory");
        if (connectionFactoryType == null)
            throw new InvalidOperationException("ConnectionFactory type not found in RabbitMQ.Client assembly.");

        var factory = Activator.CreateInstance(connectionFactoryType)!;

        // Configurar propriedades do factory
        connectionFactoryType.GetProperty("HostName")?.SetValue(factory, _settings.HostName);
        connectionFactoryType.GetProperty("Port")?.SetValue(factory, _settings.Port);
        connectionFactoryType.GetProperty("UserName")?.SetValue(factory, _settings.UserName);
        connectionFactoryType.GetProperty("Password")?.SetValue(factory, _settings.Password);
        connectionFactoryType.GetProperty("VirtualHost")?.SetValue(factory, _settings.VirtualHost);

        // Criar conexão e canal via reflexão
        var createConnectionMethod = connectionFactoryType.GetMethod("CreateConnection", Type.EmptyTypes);
        if (createConnectionMethod == null)
            throw new InvalidOperationException("CreateConnection method not found on ConnectionFactory.");

        _connection = createConnectionMethod.Invoke(factory, null)!;
        if (_connection == null) throw new InvalidOperationException("Failed to create RabbitMQ connection.");

        var createModelMethod = _connection.GetType().GetMethod("CreateModel", Type.EmptyTypes);
        if (createModelMethod == null)
            throw new InvalidOperationException("CreateModel method not found on connection object.");

        _channel = createModelMethod.Invoke(_connection, null)!;
        if (_channel == null) throw new InvalidOperationException("Failed to create RabbitMQ channel.");

        // Declarar exchange
        var exchangeDeclare = _channel.GetType().GetMethod("ExchangeDeclare", new Type[] { typeof(string), typeof(string), typeof(bool), typeof(bool) });
        if (exchangeDeclare == null)
        {
            // fallback: try any overload with string first parameter
            exchangeDeclare = _channel.GetType().GetMethod("ExchangeDeclare");
        }

        exchangeDeclare?.Invoke(_channel, new object[] { _settings.ExchangeName, "direct", true, false });

        _logger.LogInformation("RabbitMqConsumer inicializado com sucesso. Exchange: {ExchangeName}", _settings.ExchangeName);
    }

    public async Task StartConsumingAsync(string queueName, Func<string, Task> messageHandler)
    {
        await Task.Run(() =>
        {
            // Declarar fila via reflexão
            var queueDeclare = _channel.GetType().GetMethod("QueueDeclare", new Type[] { typeof(string), typeof(bool), typeof(bool), typeof(bool), typeof(System.Collections.IDictionary) })
                               ?? _channel.GetType().GetMethod("QueueDeclare", new Type[] { typeof(string), typeof(bool), typeof(bool), typeof(bool) })
                               ?? _channel.GetType().GetMethod("QueueDeclare");

            if (queueDeclare == null) throw new InvalidOperationException("QueueDeclare method not found on channel.");

            queueDeclare.Invoke(_channel, new object?[] { queueName, true, false, false, null });

            // Vincular fila ao exchange
            var queueBind = _channel.GetType().GetMethod("QueueBind");
            if (queueBind == null) throw new InvalidOperationException("QueueBind method not found on channel.");
            queueBind.Invoke(_channel, new object[] { queueName, _settings.ExchangeName, queueName.Replace(_settings.QueuePrefix, "") });

            // Criar EventingBasicConsumer via reflexão
            var consumerType = _channel.GetType().Assembly.GetType("RabbitMQ.Client.Events.EventingBasicConsumer")
                               ?? _channel.GetType().Assembly.GetType("RabbitMQ.Client.EventingBasicConsumer")
                               ?? Type.GetType("RabbitMQ.Client.Events.EventingBasicConsumer, RabbitMQ.Client");

            if (consumerType == null)
                throw new InvalidOperationException("EventingBasicConsumer type not found. Ensure RabbitMQ.Client is available at runtime.");

            var consumer = Activator.CreateInstance(consumerType, new object[] { _channel }) ?? throw new InvalidOperationException("Failed to create EventingBasicConsumer instance.");

            // Configurar o evento Received via wrapper e delegate
            var receivedEvent = consumerType.GetEvent("Received") ?? throw new InvalidOperationException("Event 'Received' not found on EventingBasicConsumer.");
            var handlerType = receivedEvent.EventHandlerType;
            var handler = Delegate.CreateDelegate(handlerType!, new MessageHandlerWrapper(queueName, messageHandler, _logger), "HandleMessage");
            receivedEvent.AddEventHandler(consumer, handler);

            // Configurar QoS via reflexão
            var basicQos = _channel.GetType().GetMethod("BasicQos");
            if (basicQos == null) throw new InvalidOperationException("BasicQos method not found on channel.");
            basicQos.Invoke(_channel, new object[] { (uint)0, (ushort)1, false });

            // BasicConsume
            var basicConsume = _channel.GetType().GetMethod("BasicConsume");
            if (basicConsume == null) throw new InvalidOperationException("BasicConsume method not found on channel.");
            var consumeResult = basicConsume.Invoke(_channel, new object[] { queueName, false, consumer });
            _consumerTag = consumeResult as string;
        });
    }

    public async Task StopConsumingAsync()
    {
        await Task.Run(() =>
        {
            if (!string.IsNullOrEmpty(_consumerTag))
            {
                var basicCancel = _channel.GetType().GetMethod("BasicCancel");
                if (basicCancel != null) basicCancel.Invoke(_channel, new object[] { _consumerTag });
                _consumerTag = null;
            }
        });
    }

    public void Dispose()
    {
        try { StopConsumingAsync().Wait(); } catch { }

        try
        {
            var closeMethod = _channel.GetType().GetMethod("Close");
            closeMethod?.Invoke(_channel, null);
        }
        catch { }

        try
        {
            var closeConn = _connection.GetType().GetMethod("Close");
            closeConn?.Invoke(_connection, null);
        }
        catch { }

        if (_channel is IDisposable ch) ch.Dispose();
        if (_connection is IDisposable conn) conn.Dispose();
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

    // assinatura esperada: void Handler(object sender, BasicDeliverEventArgs ea)
    public void HandleMessage(object sender, object eventArgs)
    {
        try
        {
            var bodyProp = eventArgs.GetType().GetProperty("Body");
            object? rawBody = bodyProp?.GetValue(eventArgs);

            byte[]? bodyBytes = null;
            if (rawBody is byte[] bArr) bodyBytes = bArr;
            else if (rawBody is ReadOnlyMemory<byte> rom) bodyBytes = rom.ToArray();
            else if (rawBody is ArraySegment<byte> seg) bodyBytes = seg.ToArray();
            else if (rawBody is Memory<byte> mem) bodyBytes = mem.ToArray();

            if (bodyBytes == null) throw new InvalidOperationException("Unable to extract message body from event args.");

            var message = Encoding.UTF8.GetString(bodyBytes);

            var deliveryTagProp = eventArgs.GetType().GetProperty("DeliveryTag");
            var deliveryTagObj = deliveryTagProp?.GetValue(eventArgs);
            var deliveryTag = deliveryTagObj is ulong ul ? ul : Convert.ToUInt64(deliveryTagObj);

            _logger.LogInformation("Mensagem recebida. DeliveryTag: {DeliveryTag}, Tamanho: {Size} bytes", deliveryTag, bodyBytes.Length);

            Task.Run(async () =>
            {
                try
                {
                    await _messageHandler(message);

                    var channel = sender.GetType().GetProperty("Model")?.GetValue(sender);
                    var basicAck = channel?.GetType().GetMethod("BasicAck");
                    basicAck?.Invoke(channel, new object[] { deliveryTag, false });
                    _logger.LogInformation("Mensagem processada com sucesso. DeliveryTag: {DeliveryTag}", deliveryTag);
                }
                catch (Exception ex)
                {
                    var channel = sender.GetType().GetProperty("Model")?.GetValue(sender);
                    var dlqName = $"{_queueName}.dlq";
                    var queueDeclare = channel?.GetType().GetMethod("QueueDeclare");
                    queueDeclare?.Invoke(channel, new object?[] { dlqName, true, false, false, null });

                    var createProps = channel?.GetType().GetMethod("CreateBasicProperties");
                    var properties = createProps?.Invoke(channel, null);
                    if (properties != null)
                    {
                        var persistentProp = properties.GetType().GetProperty("Persistent");
                        persistentProp?.SetValue(properties, true);

                        var headersProp = properties.GetType().GetProperty("Headers");
                        headersProp?.SetValue(properties, new Dictionary<string, object>
                        {
                            ["x-original-queue"] = _queueName,
                            ["x-error-message"] = ex.Message,
                            ["x-error-timestamp"] = DateTime.UtcNow.ToString("O")
                        });
                    }

                    var basicPublish = channel?.GetType().GetMethod("BasicPublish");
                    basicPublish?.Invoke(channel, new object?[] { "", dlqName, properties, bodyBytes });

                    var basicNack = channel?.GetType().GetMethod("BasicNack");
                    basicNack?.Invoke(channel, new object?[] { deliveryTag, false, false });
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

