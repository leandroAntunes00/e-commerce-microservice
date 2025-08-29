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

        if (order.Status != "Pending")
        {
            throw new InvalidOperationException("Only pending orders can be confirmed");
        }

        order.Status = "Confirmed";
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
