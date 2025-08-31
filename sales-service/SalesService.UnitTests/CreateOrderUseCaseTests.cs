using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using SalesService.Application.UseCases;
using SalesService.Application.Dtos;
using SalesService.Domain.Interfaces;
using SalesService.Domain.Entities;
using SalesService.Services;
using Messaging;
using Messaging.Events;
using System.Collections.Generic;
using System.Linq;

namespace SalesService.UnitTests;

public class CreateOrderUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldCreateOrderAndPublishEvent()
    {
        // Arrange
        var orderRepoMock = new Mock<IOrderRepository>();
        var stockClientMock = new Mock<IStockServiceClient>();
        var msgPublisherMock = new Mock<IMessagePublisher>();

        var product = new StockProductResponse { Id = 1, Name = "Prod", Price = 10m, StockQuantity = 100 };
        stockClientMock.Setup(s => s.GetProductAsync(1)).ReturnsAsync(product);

        var createdOrder = new Order
        {
            Id = 42,
            UserId = 5,
            TotalAmount = 20m,
            Items = new List<OrderItem>
        {
            new OrderItem { ProductId = 1, ProductName = "Prod", UnitPrice = 10m, Quantity = 2, TotalPrice = 20m }
        }
        };

        orderRepoMock.Setup(r => r.CreateAsync(It.IsAny<Order>())).ReturnsAsync(createdOrder);

        // Arrange mapper
        var mapper = SalesService.UnitTests.TestHelpers.TestMapperFactory.CreateMapper();

        var useCase = new CreateOrderUseCase(orderRepoMock.Object, stockClientMock.Object, msgPublisherMock.Object, mapper);

        var command = new CreateOrderCommand { UserId = 5, Items = new List<OrderItemCommand> { new OrderItemCommand { ProductId = 1, Quantity = 2 } } };

        // Act
        var resultId = await useCase.ExecuteAsync(command);

        // Assert
        resultId.Should().Be(42);
        orderRepoMock.Verify(r => r.CreateAsync(It.Is<Order>(o => o.UserId == 5 && o.Items.Count == 1 && o.TotalAmount == 20m)), Times.Once);
        msgPublisherMock.Verify(m => m.PublishAsync(It.IsAny<OrderCreatedEvent>()), Times.Once);
    }
}
