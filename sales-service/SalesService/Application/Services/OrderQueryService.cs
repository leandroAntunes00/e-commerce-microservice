using AutoMapper;
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
    private readonly IMapper _mapper;

    public OrderQueryService(IOrderRepository orderRepository, IMapper mapper)
    {
        _orderRepository = orderRepository;
        _mapper = mapper;
    }

    public async Task<OrderResponse?> GetOrderByIdAsync(int id, int userId)
    {
        var order = await _orderRepository.GetByIdAndUserIdAsync(id, userId);
        if (order == null) return null;

        return _mapper.Map<OrderResponse>(order);
    }

    public async Task<OrdersListResponse> GetOrdersByUserIdAsync(int userId)
    {
        var orders = await _orderRepository.GetByUserIdAsync(userId);
        var orderResponses = orders.Select(o => _mapper.Map<OrderResponse>(o)).ToList();

        return new OrdersListResponse { Orders = orderResponses };
    }
}
