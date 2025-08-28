using System;
using System.Threading;
using System.Threading.Tasks;
using Messaging;
using Messaging.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StockService.Data;
using Microsoft.EntityFrameworkCore;

namespace StockService.Services;

public class OrderEventConsumerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly RabbitMqSettings _rabbitMqSettings;
    private readonly ILogger<OrderEventConsumerService> _logger;

    public OrderEventConsumerService(
        IServiceProvider serviceProvider,
        Microsoft.Extensions.Options.IOptions<RabbitMqSettings> rabbitMqSettings,
        ILogger<OrderEventConsumerService> logger)
    {
        _serviceProvider = serviceProvider;
        _rabbitMqSettings = rabbitMqSettings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Iniciando consumidor de eventos de pedido...");

        // Criar consumer com retry para não derrubar o host se RabbitMQ ainda não estiver pronto
        IMessageConsumer? consumer = null;
        int retryCount = 0;
        const int maxRetries = 30; // Máximo de 30 tentativas (60 segundos)

    while (!stoppingToken.IsCancellationRequested && consumer == null && retryCount < maxRetries)
        {
            try
            {
        // Resolve consumer from DI inside a scope so its dependencies are injected
        using var scope = _serviceProvider.CreateScope();
        consumer = scope.ServiceProvider.GetRequiredService<IMessageConsumer>();
        _logger.LogInformation("Consumidor RabbitMQ resolvido via DI com sucesso.");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                retryCount++;
                _logger.LogWarning(ex, "Não foi possível conectar ao RabbitMQ. Tentativa {Retry}/{MaxRetries}. Tentando novamente em 2s...", retryCount, maxRetries);
                try { await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken); } catch (OperationCanceledException) { break; }
            }
        }

        if (consumer == null)
        {
            _logger.LogError("Não foi possível inicializar o consumidor de eventos de pedido; saindo.");
            return;
        }

        try
        {
            // Consumir eventos OrderCreated
            await consumer.StartConsumingAsync(
                queueName: $"{_rabbitMqSettings.QueuePrefix}order_created",
                messageHandler: async (message) =>
                {
                    try
                    {
                        var orderEvent = System.Text.Json.JsonSerializer.Deserialize<OrderCreatedEvent>(message);
                        if (orderEvent != null)
                        {
                            await ProcessOrderCreatedEvent(orderEvent);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao processar evento OrderCreatedEvent");
                    }
                });

            // Consumir eventos OrderCancelled
            await consumer.StartConsumingAsync(
                queueName: $"{_rabbitMqSettings.QueuePrefix}order_cancelled",
                messageHandler: async (message) =>
                {
                    try
                    {
                        var orderEvent = System.Text.Json.JsonSerializer.Deserialize<OrderCancelledEvent>(message);
                        if (orderEvent != null)
                        {
                            await ProcessOrderCancelledEvent(orderEvent);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao processar evento OrderCancelledEvent");
                    }
                });

            // Aguardar até o serviço ser parado
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        finally
        {
            try
            {
                if (consumer != null)
                {
                    await consumer.StopConsumingAsync();
                    (consumer as IDisposable)?.Dispose();
                    _logger.LogInformation("Consumidor RabbitMQ finalizado.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao finalizar consumidor RabbitMQ");
            }
        }
    }

    private async Task ProcessOrderCreatedEvent(OrderCreatedEvent orderEvent)
    {
        _logger.LogInformation($"Processando pedido criado: OrderId={orderEvent.OrderId}, UserId={orderEvent.UserId}");

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StockDbContext>();

        foreach (var item in orderEvent.Items)
        {
            var product = await context.Products.FindAsync(item.ProductId);
            if (product == null)
            {
                _logger.LogWarning($"Produto não encontrado: ProductId={item.ProductId}");
                continue;
            }

            if (product.StockQuantity < item.Quantity)
            {
                _logger.LogError($"Estoque insuficiente para produto {product.Name}: solicitado={item.Quantity}, disponível={product.StockQuantity}");

                // Poderia publicar um evento de falha de reserva aqui
                continue;
            }

            // Reservar estoque
            var previousStock = product.StockQuantity;
            product.StockQuantity -= item.Quantity;
            product.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            _logger.LogInformation($"Estoque reservado para produto {product.Name}: {previousStock} -> {product.StockQuantity}");

            // Publicar evento de atualização de estoque
            var messagePublisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();
            var stockUpdatedEvent = new StockUpdatedEvent
            {
                ProductId = product.Id,
                ProductName = product.Name,
                PreviousStock = previousStock,
                NewStock = product.StockQuantity,
                Operation = "Reserved",
                UpdatedAt = product.UpdatedAt ?? DateTime.UtcNow
            };

            await messagePublisher.PublishAsync(stockUpdatedEvent);
        }

        _logger.LogInformation($"Pedido {orderEvent.OrderId} processado com sucesso");
    }

    private async Task ProcessOrderCancelledEvent(OrderCancelledEvent orderEvent)
    {
        _logger.LogInformation($"Processando cancelamento de pedido: OrderId={orderEvent.OrderId}, UserId={orderEvent.UserId}");

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StockDbContext>();

        foreach (var item in orderEvent.Items)
        {
            var product = await context.Products.FindAsync(item.ProductId);
            if (product == null)
            {
                _logger.LogWarning($"Produto não encontrado: ProductId={item.ProductId}");
                continue;
            }

            // Liberar estoque
            var previousStock = product.StockQuantity;
            product.StockQuantity += item.Quantity;
            product.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            _logger.LogInformation($"Estoque liberado para produto {product.Name}: {previousStock} -> {product.StockQuantity}");

            // Publicar evento de atualização de estoque
            var messagePublisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();
            var stockUpdatedEvent = new StockUpdatedEvent
            {
                ProductId = product.Id,
                ProductName = product.Name,
                PreviousStock = previousStock,
                NewStock = product.StockQuantity,
                Operation = "Released",
                UpdatedAt = product.UpdatedAt ?? DateTime.UtcNow
            };

            await messagePublisher.PublishAsync(stockUpdatedEvent);
        }

        _logger.LogInformation($"Cancelamento do pedido {orderEvent.OrderId} processado com sucesso");
    }
}
