using SalesService.Application.Dtos;
using SalesService.Domain.Interfaces;
using Messaging;
using Messaging.Events;
using AutoMapper;

namespace SalesService.Application.UseCases;

public interface IProcessPaymentUseCase
{
    Task ExecuteAsync(ProcessPaymentCommand command);
}

public class ProcessPaymentUseCase : IProcessPaymentUseCase
{
    private readonly IOrderRepository _orderRepository;
    private readonly IMessagePublisher _messagePublisher;
    private readonly IMapper _mapper;

    public ProcessPaymentUseCase(
        IOrderRepository orderRepository,
        IMessagePublisher messagePublisher,
        IMapper mapper)
    {
        _orderRepository = orderRepository;
        _messagePublisher = messagePublisher;
        _mapper = mapper;
    }

    public async Task ExecuteAsync(ProcessPaymentCommand command)
    {
        var order = await _orderRepository.GetByIdAndUserIdAsync(command.OrderId, command.UserId);
        if (order == null)
        {
            throw new ArgumentException("Order not found");
        }

        // Validar se o valor do pagamento corresponde ao total do pedido
        if (command.Amount != order.TotalAmount)
        {
            throw new InvalidOperationException("Payment amount does not match order total");
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
    var evt = _mapper.Map<OrderConfirmedEvent>(order);
    evt.Method = command.PaymentMethod;

    await _messagePublisher.PublishAsync(evt);
    }
}
