using SalesService.Api.Dtos;
using SalesService.Domain.Entities;
using SalesService.Domain.Interfaces;

namespace SalesService.Application.Services;

public interface IOrderQueryService
{
    Task<OrderResponse?> GetOrderByIdAsync(int id, int userId);
    Task<OrdersListResponse> GetOrdersByUserIdAsync(int userId);
}

public class OrderQueryService : IOrderQueryService
{
    private readonly IOrderRepository _orderRepository;

    public OrderQueryService(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<OrderResponse?> GetOrderByIdAsync(int id, int userId)
    {
        var order = await _orderRepository.GetByIdAndUserIdAsync(id, userId);
        if (order == null) return null;

        return MapToOrderResponse(order);
    }

    public async Task<OrdersListResponse> GetOrdersByUserIdAsync(int userId)
    {
        var orders = await _orderRepository.GetByUserIdAsync(userId);
        var orderResponses = orders.Select(MapToOrderResponse).ToList();

        return new OrdersListResponse { Orders = orderResponses };
    }

    private static OrderResponse MapToOrderResponse(Order order)
    {
        return new OrderResponse
        {
            Id = order.Id,
            UserId = order.UserId,
            Status = order.Status,
            TotalAmount = order.TotalAmount,
            Notes = order.Notes,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            Items = order.Items.Select(i => new OrderItemResponse
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity,
                TotalPrice = i.TotalPrice,
                CreatedAt = i.CreatedAt
            }).ToList()
        };
    }
}
