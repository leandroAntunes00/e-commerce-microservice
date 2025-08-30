using SalesService.Application.Dtos;
using SalesService.Domain.Interfaces;
using Messaging;
using Messaging.Events;

namespace SalesService.Application.UseCases;

public interface ICancelOrderUseCase
{
    Task ExecuteAsync(CancelOrderCommand command);
}

public class CancelOrderUseCase : ICancelOrderUseCase
{
    private readonly IOrderRepository _orderRepository;
    private readonly IMessagePublisher _messagePublisher;

    public CancelOrderUseCase(IOrderRepository orderRepository, IMessagePublisher messagePublisher)
    {
        _orderRepository = orderRepository;
        _messagePublisher = messagePublisher;
    }

    public async Task ExecuteAsync(CancelOrderCommand command)
    {
        var order = await _orderRepository.GetByIdAndUserIdAsync(command.OrderId, command.UserId);
        if (order == null)
        {
            throw new ArgumentException("Order not found");
        }

        var pending = SalesService.Domain.Enums.OrderStatus.Pending.ToString();
        var reserved = SalesService.Domain.Enums.OrderStatus.Reserved.ToString();

        if (order.Status != pending && order.Status != reserved)
        {
            throw new InvalidOperationException("Only pending or reserved orders can be cancelled");
        }

        order.Status = SalesService.Domain.Enums.OrderStatus.Cancelled.ToString();
        await _orderRepository.UpdateAsync(order);

        // Publish OrderCancelledEvent so other services (e.g., Stock) can react and release reserved stock
        var evt = new OrderCancelledEvent
        {
            OrderId = order.Id,
            UserId = order.UserId,
            Items = order.Items.Select(i => new OrderItemEvent
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList(),
            CancelledAt = DateTime.UtcNow
        };

        await _messagePublisher.PublishAsync(evt);
    }
}
