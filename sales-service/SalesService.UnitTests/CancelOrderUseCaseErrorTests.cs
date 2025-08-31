using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using SalesService.Application.UseCases;
using SalesService.Application.Dtos;
using SalesService.Domain.Interfaces;
using SalesService.Domain.Entities;
using Messaging.Events;
using System.Collections.Generic;

namespace SalesService.UnitTests;

public class CancelOrderUseCaseErrorTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldThrowArgumentException_WhenOrderNotFound()
    {
        // Arrange
        var orderRepoMock = new Mock<IOrderRepository>();
        var msgPublisherMock = new Mock<Messaging.IMessagePublisher>();

        orderRepoMock.Setup(r => r.GetByIdAndUserIdAsync(99, 1)).ReturnsAsync((Order?)null);

        var mapper = SalesService.UnitTests.TestHelpers.TestMapperFactory.CreateMapper();

        var useCase = new CancelOrderUseCase(orderRepoMock.Object, msgPublisherMock.Object, mapper);

        var command = new CancelOrderCommand { OrderId = 99, UserId = 1 };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(command));
        ex.Message.Should().Be("Order not found");

        orderRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Never);
        msgPublisherMock.Verify(m => m.PublishAsync(It.IsAny<OrderCancelledEvent>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowInvalidOperationException_WhenOrderInNonCancelableStatus()
    {
        // Arrange
        var orderRepoMock = new Mock<IOrderRepository>();
        var msgPublisherMock = new Mock<Messaging.IMessagePublisher>();

        var order = new Order { Id = 20, UserId = 2, Status = SalesService.Domain.Enums.OrderStatus.Confirmed.ToString(), Items = new List<OrderItem>() };
        orderRepoMock.Setup(r => r.GetByIdAndUserIdAsync(20, 2)).ReturnsAsync(order);

        var mapper2 = SalesService.UnitTests.TestHelpers.TestMapperFactory.CreateMapper();

        var useCase = new CancelOrderUseCase(orderRepoMock.Object, msgPublisherMock.Object, mapper2);

        var command = new CancelOrderCommand { OrderId = 20, UserId = 2 };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync(command));
        ex.Message.Should().Be("Only pending or reserved orders can be cancelled");

        orderRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Never);
        msgPublisherMock.Verify(m => m.PublishAsync(It.IsAny<OrderCancelledEvent>()), Times.Never);
    }
}

