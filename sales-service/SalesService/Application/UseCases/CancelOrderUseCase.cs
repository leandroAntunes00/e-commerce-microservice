using SalesService.Application.Dtos;
using SalesService.Domain.Interfaces;

namespace SalesService.Application.UseCases;

public interface ICancelOrderUseCase
{
    Task ExecuteAsync(CancelOrderCommand command);
}

public class CancelOrderUseCase : ICancelOrderUseCase
{
    private readonly IOrderRepository _orderRepository;

    public CancelOrderUseCase(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task ExecuteAsync(CancelOrderCommand command)
    {
        var order = await _orderRepository.GetByIdAndUserIdAsync(command.OrderId, command.UserId);
        if (order == null)
        {
            throw new ArgumentException("Order not found");
        }

        if (order.Status != "Pending")
        {
            throw new InvalidOperationException("Only pending orders can be cancelled");
        }

        order.Status = "Cancelled";
        await _orderRepository.UpdateAsync(order);
    }
}
