using SalesService.Application.Dtos;
using SalesService.Domain.Entities;
using SalesService.Domain.Interfaces;
using SalesService.Services;
using Messaging;
using Messaging.Events;

namespace SalesService.Application.UseCases;

public interface ICreateOrderUseCase
{
    Task<int> ExecuteAsync(CreateOrderCommand command);
}

public class CreateOrderUseCase : ICreateOrderUseCase
{
    private readonly IOrderRepository _orderRepository;
    private readonly IStockServiceClient _stockServiceClient;
    private readonly IMessagePublisher _messagePublisher;

    public CreateOrderUseCase(
        IOrderRepository orderRepository,
        IStockServiceClient stockServiceClient,
        IMessagePublisher messagePublisher)
    {
        _orderRepository = orderRepository;
        _stockServiceClient = stockServiceClient;
        _messagePublisher = messagePublisher;
    }

    public async Task<int> ExecuteAsync(CreateOrderCommand command)
    {
        // Validate and get product details
        decimal totalAmount = 0;
        var orderItems = new List<OrderItem>();

        foreach (var item in command.Items)
        {
            var product = await _stockServiceClient.GetProductAsync(item.ProductId);
            if (product == null)
            {
                throw new ArgumentException($"Product not found (ID: {item.ProductId})");
            }

            var itemTotal = product.Price * item.Quantity;
            totalAmount += itemTotal;

            orderItems.Add(new OrderItem
            {
                ProductId = item.ProductId,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = item.Quantity,
                TotalPrice = itemTotal
            });
        }

        // Create order
        var order = new Order
        {
            UserId = command.UserId,
            Status = "Pending",
            TotalAmount = totalAmount,
            Notes = command.Notes,
            Items = orderItems
        };

        var createdOrder = await _orderRepository.CreateAsync(order);

        // Publish event
        var orderCreatedEvent = new OrderCreatedEvent
        {
            OrderId = createdOrder.Id,
            UserId = command.UserId,
            Items = createdOrder.Items.Select(i => new OrderItemEvent
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList(),
            TotalAmount = createdOrder.TotalAmount,
            CreatedAt = createdOrder.CreatedAt
        };

        await _messagePublisher.PublishAsync(orderCreatedEvent);

        return createdOrder.Id;
    }
}
