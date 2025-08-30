using SalesService.Application.Dtos;
using SalesService.Domain.Interfaces;
using Messaging;
using Messaging.Events;

namespace SalesService.Application.UseCases;

public interface IProcessPaymentUseCase
{
    Task ExecuteAsync(ProcessPaymentCommand command);
}

public class ProcessPaymentUseCase : IProcessPaymentUseCase
{
    private readonly IOrderRepository _orderRepository;
    private readonly IMessagePublisher _messagePublisher;

    public ProcessPaymentUseCase(
        IOrderRepository orderRepository,
        IMessagePublisher messagePublisher)
    {
        _orderRepository = orderRepository;
        _messagePublisher = messagePublisher;
    }

    public async Task ExecuteAsync(ProcessPaymentCommand command)
    {
        var order = await _orderRepository.GetByIdAndUserIdAsync(command.OrderId, command.UserId);
        if (order == null)
        {
            throw new ArgumentException("Order not found");
        }

        // Permitir confirmar pedidos apenas quando estiverem 'Reserved'
        var reserved = SalesService.Domain.Enums.OrderStatus.Reserved.ToString();
        if (order.Status != reserved)
        {
            throw new InvalidOperationException("Only reserved orders can be confirmed");
        }

        order.Status = SalesService.Domain.Enums.OrderStatus.Confirmed.ToString();
        await _orderRepository.UpdateAsync(order);

        // Publish confirmation event
        var evt = new OrderConfirmedEvent
        {
            OrderId = order.Id,
            UserId = order.UserId,
            ConfirmedAt = order.UpdatedAt ?? DateTime.UtcNow,
            Method = command.PaymentMethod
        };

        await _messagePublisher.PublishAsync(evt);
    }
}
