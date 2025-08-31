using SalesService.Application.Dtos;
using SalesService.Domain.Interfaces;
using Messaging;
using Messaging.Events;
using AutoMapper;

namespace SalesService.Application.UseCases;

public interface ICancelOrderUseCase
{
    Task ExecuteAsync(CancelOrderCommand command);
}

public class CancelOrderUseCase : ICancelOrderUseCase
{
    private readonly IOrderRepository _orderRepository;
    private readonly IMessagePublisher _messagePublisher;
    private readonly IMapper _mapper;

    public CancelOrderUseCase(IOrderRepository orderRepository, IMessagePublisher messagePublisher, IMapper mapper)
    {
        _orderRepository = orderRepository;
        _messagePublisher = messagePublisher;
        _mapper = mapper;
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
    var evt = _mapper.Map<OrderCancelledEvent>(order);
    evt.CancelledAt = DateTime.UtcNow;
    await _messagePublisher.PublishAsync(evt);
    }
}
