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

namespace SalesService.UnitTests;

public class ProcessPaymentUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldConfirmReservedOrderAndPublishEvent()
    {
        // Arrange
        var orderRepoMock = new Mock<IOrderRepository>();
        var msgPublisherMock = new Mock<Messaging.IMessagePublisher>();

        var order = new Order { Id = 11, UserId = 3, Status = SalesService.Domain.Enums.OrderStatus.Reserved.ToString(), TotalAmount = 100m, UpdatedAt = null };
        orderRepoMock.Setup(r => r.GetByIdAndUserIdAsync(11, 3)).ReturnsAsync(order);
        orderRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);

        var useCase = new ProcessPaymentUseCase(orderRepoMock.Object, msgPublisherMock.Object);

        var command = new ProcessPaymentCommand { OrderId = 11, UserId = 3, PaymentMethod = "PIX", Amount = 100m };

        // Act
        await useCase.ExecuteAsync(command);

        // Assert
        order.Status.Should().Be(SalesService.Domain.Enums.OrderStatus.Confirmed.ToString());
        orderRepoMock.Verify(r => r.UpdateAsync(It.Is<Order>(o => o.Status == SalesService.Domain.Enums.OrderStatus.Confirmed.ToString())), Times.Once);
        msgPublisherMock.Verify(m => m.PublishAsync(It.IsAny<OrderConfirmedEvent>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowException_WhenPaymentAmountDoesNotMatchOrderTotal()
    {
        // Arrange
        var orderRepoMock = new Mock<IOrderRepository>();
        var msgPublisherMock = new Mock<Messaging.IMessagePublisher>();

        var order = new Order { Id = 12, UserId = 3, Status = SalesService.Domain.Enums.OrderStatus.Reserved.ToString(), TotalAmount = 200m, UpdatedAt = null };
        orderRepoMock.Setup(r => r.GetByIdAndUserIdAsync(12, 3)).ReturnsAsync(order);

        var useCase = new ProcessPaymentUseCase(orderRepoMock.Object, msgPublisherMock.Object);

        var command = new ProcessPaymentCommand { OrderId = 12, UserId = 3, PaymentMethod = "Card", Amount = 100m };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync(command));
        exception.Message.Should().Be("Payment amount does not match order total");
    }
}
