using System;
using System.Threading;
using System.Threading.Tasks;
using Messaging;
using Messaging.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SalesService.Data;
using Microsoft.EntityFrameworkCore;

namespace SalesService.Services;

public class ReservationResultConsumerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly RabbitMqSettings _rabbitMqSettings;
    private readonly ILogger<ReservationResultConsumerService> _logger;

    public ReservationResultConsumerService(
        IServiceProvider serviceProvider,
        Microsoft.Extensions.Options.IOptions<RabbitMqSettings> rabbitMqSettings,
        ILogger<ReservationResultConsumerService> logger)
    {
        _serviceProvider = serviceProvider;
        _rabbitMqSettings = rabbitMqSettings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Iniciando consumidor de resultados de reserva...");

        IMessageConsumer? consumer = null;
        int retryCount = 0;
        const int maxRetries = 30;

        while (!stoppingToken.IsCancellationRequested && consumer == null && retryCount < maxRetries)
        {
            try
            {
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
                _logger.LogWarning(ex, "Não foi possível conectar ao RabbitMQ. Tentativa {Retry}/{MaxRetries}.", retryCount, maxRetries);
                try { await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken); } catch (OperationCanceledException) { break; }
            }
        }

        if (consumer == null)
        {
            _logger.LogError("Não foi possível inicializar o consumidor de resultados de reserva; saindo.");
            return;
        }

        try
        {
            await consumer.StartConsumingAsync(
                queueName: $"{_rabbitMqSettings.QueuePrefix}order_reservation_completed",
                messageHandler: async (message) =>
                {
                    try
                    {
                        var evt = System.Text.Json.JsonSerializer.Deserialize<OrderReservationCompletedEvent>(message);
                        if (evt != null)
                        {
                            await ProcessReservationResult(evt);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao processar OrderReservationCompletedEvent");
                    }
                });

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

    private async Task ProcessReservationResult(OrderReservationCompletedEvent evt)
    {
        _logger.LogInformation($"Processando resultado de reserva: OrderId={evt.OrderId}, Success={evt.Success}");

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SalesDbContext>();

        var order = await context.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == evt.OrderId);
        if (order == null)
        {
            _logger.LogWarning($"Order not found: {evt.OrderId}");
            return;
        }

        if (evt.Success)
        {
            order.Status = "Reserved"; // indicates stock reserved
            order.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            _logger.LogInformation($"Order {evt.OrderId} marked as Reserved");
        }
        else
        {
            order.Status = "Cancelled";
            order.Notes = evt.Reason;
            order.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            _logger.LogInformation($"Order {evt.OrderId} Cancelled due reservation failure: {evt.Reason}");

            // Optionally publish OrderCancelledEvent so StockService or others can react (existing flow)
            var publisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();
            var cancelEvt = new OrderCancelledEvent
            {
                OrderId = order.Id,
                UserId = order.UserId,
                CancelledAt = DateTime.UtcNow,
                Items = order.Items.Select(i => new OrderItemEvent { ProductId = i.ProductId, ProductName = i.ProductName, Quantity = i.Quantity, UnitPrice = i.UnitPrice }).ToList()
            };

            await publisher.PublishAsync(cancelEvt);
        }
    }
}
