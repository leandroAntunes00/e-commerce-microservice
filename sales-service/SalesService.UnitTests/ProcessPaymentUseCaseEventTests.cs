using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using AutoMapper;
using Messaging;
using Messaging.Events;
using SalesService.Application.UseCases;
using SalesService.Domain.Entities;
using SalesService.Domain.Interfaces;

namespace SalesService.UnitTests;

public class ProcessPaymentUseCaseEventTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldPublishEvent_WithMethodSet()
    {
        // Arrange
        var orderRepoMock = new Mock<IOrderRepository>();
        var msgPublisherMock = new Mock<IMessagePublisher>();

        var order = new Order { Id = 200, UserId = 7, Status = SalesService.Domain.Enums.OrderStatus.Reserved.ToString(), TotalAmount = 50m, UpdatedAt = null };
        orderRepoMock.Setup(r => r.GetByIdAndUserIdAsync(200, 7)).ReturnsAsync(order);
        orderRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);

    var mapper = SalesService.UnitTests.TestHelpers.TestMapperFactory.CreateMapper();

        var useCase = new ProcessPaymentUseCase(orderRepoMock.Object, msgPublisherMock.Object, mapper);

        var command = new SalesService.Application.Dtos.ProcessPaymentCommand { OrderId = 200, UserId = 7, PaymentMethod = "CARD", Amount = 50m };

        // Act
        await useCase.ExecuteAsync(command);

        // Assert
        msgPublisherMock.Verify(m => m.PublishAsync(It.Is<OrderConfirmedEvent>(e => e.OrderId == 200 && e.Method == "CARD")), Times.Once);
    }
}
