using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using SalesService.Application.UseCases;
using SalesService.Application.Dtos;
using SalesService.Domain.Interfaces;
using SalesService.Domain.Entities;
using SalesService.Services;
using Messaging.Events;
using System.Collections.Generic;

namespace SalesService.UnitTests;

public class ErrorCasesTests
{
    [Fact]
    public async Task CreateOrder_WhenProductNotFound_ShouldThrowArgumentException()
    {
        var orderRepoMock = new Mock<IOrderRepository>();
        var stockClientMock = new Mock<IStockServiceClient>();
        var msgPublisherMock = new Mock<Messaging.IMessagePublisher>();

        stockClientMock.Setup(s => s.GetProductAsync(99)).ReturnsAsync((StockProductResponse?)null);

        var useCase = new CreateOrderUseCase(orderRepoMock.Object, stockClientMock.Object, msgPublisherMock.Object);
        var command = new CreateOrderCommand { UserId = 1, Items = new List<OrderItemCommand> { new OrderItemCommand { ProductId = 99, Quantity = 1 } } };

        await useCase.Invoking(u => u.ExecuteAsync(command)).Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CancelOrder_WhenOrderNotFound_ShouldThrowArgumentException()
    {
        var orderRepoMock = new Mock<IOrderRepository>();
        var msgPublisherMock = new Mock<Messaging.IMessagePublisher>();

        orderRepoMock.Setup(r => r.GetByIdAndUserIdAsync(5, 2)).ReturnsAsync((Order?)null);

        var useCase = new CancelOrderUseCase(orderRepoMock.Object, msgPublisherMock.Object);
        var command = new CancelOrderCommand { OrderId = 5, UserId = 2 };

        await useCase.Invoking(u => u.ExecuteAsync(command)).Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CancelOrder_WhenStatusInvalid_ShouldThrowInvalidOperationException()
    {
        var orderRepoMock = new Mock<IOrderRepository>();
        var msgPublisherMock = new Mock<Messaging.IMessagePublisher>();

        var order = new Order { Id = 6, UserId = 3, Status = SalesService.Domain.Enums.OrderStatus.Confirmed.ToString() };
        orderRepoMock.Setup(r => r.GetByIdAndUserIdAsync(6, 3)).ReturnsAsync(order);

        var useCase = new CancelOrderUseCase(orderRepoMock.Object, msgPublisherMock.Object);
        var command = new CancelOrderCommand { OrderId = 6, UserId = 3 };

        await useCase.Invoking(u => u.ExecuteAsync(command)).Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ProcessPayment_WhenOrderNotFound_ShouldThrowArgumentException()
    {
        var orderRepoMock = new Mock<IOrderRepository>();
        var msgPublisherMock = new Mock<Messaging.IMessagePublisher>();

        orderRepoMock.Setup(r => r.GetByIdAndUserIdAsync(7, 4)).ReturnsAsync((Order?)null);

        var useCase = new ProcessPaymentUseCase(orderRepoMock.Object, msgPublisherMock.Object);
        var command = new ProcessPaymentCommand { OrderId = 7, UserId = 4, PaymentMethod = "CARD", Amount = 10m };

        await useCase.Invoking(u => u.ExecuteAsync(command)).Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ProcessPayment_WhenStatusNotReserved_ShouldThrowInvalidOperationException()
    {
        var orderRepoMock = new Mock<IOrderRepository>();
        var msgPublisherMock = new Mock<Messaging.IMessagePublisher>();

        var order = new Order { Id = 8, UserId = 5, Status = SalesService.Domain.Enums.OrderStatus.Pending.ToString(), TotalAmount = 20m };
        orderRepoMock.Setup(r => r.GetByIdAndUserIdAsync(8, 5)).ReturnsAsync(order);

        var useCase = new ProcessPaymentUseCase(orderRepoMock.Object, msgPublisherMock.Object);
        var command = new ProcessPaymentCommand { OrderId = 8, UserId = 5, PaymentMethod = "PIX", Amount = 20m };

        await useCase.Invoking(u => u.ExecuteAsync(command)).Should().ThrowAsync<InvalidOperationException>();
    }
}
