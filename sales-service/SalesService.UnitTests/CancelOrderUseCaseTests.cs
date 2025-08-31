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

public class CancelOrderUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldCancelPendingOrderAndPublishEvent()
    {
        // Arrange
        var orderRepoMock = new Mock<IOrderRepository>();
        var msgPublisherMock = new Mock<Messaging.IMessagePublisher>();

        var order = new Order
        {
            Id = 10,
            UserId = 2,
            Status = SalesService.Domain.Enums.OrderStatus.Pending.ToString(),
            Items = new List<OrderItem>
        {
            new OrderItem { ProductId = 1, ProductName = "P", Quantity = 1, UnitPrice = 5m, TotalPrice = 5m }
        }
        };

        orderRepoMock.Setup(r => r.GetByIdAndUserIdAsync(10, 2)).ReturnsAsync(order);
        orderRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);

    var mapper = SalesService.UnitTests.TestHelpers.TestMapperFactory.CreateMapper();

        var useCase = new CancelOrderUseCase(orderRepoMock.Object, msgPublisherMock.Object, mapper);

        var command = new CancelOrderCommand { OrderId = 10, UserId = 2 };

        // Act
        await useCase.ExecuteAsync(command);

        // Assert
        order.Status.Should().Be(SalesService.Domain.Enums.OrderStatus.Cancelled.ToString());
        orderRepoMock.Verify(r => r.UpdateAsync(It.Is<Order>(o => o.Status == SalesService.Domain.Enums.OrderStatus.Cancelled.ToString())), Times.Once);
        msgPublisherMock.Verify(m => m.PublishAsync(It.IsAny<OrderCancelledEvent>()), Times.Once);
    }
}
