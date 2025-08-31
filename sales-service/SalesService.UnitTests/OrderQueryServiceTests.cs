using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using AutoMapper;
using SalesService.Application.Services;
using SalesService.Domain.Interfaces;
using SalesService.Domain.Entities;
using SalesService.Api.Dtos;
using SalesService.Application.Mappings;

namespace SalesService.UnitTests;

public class OrderQueryServiceTests
{

    [Fact]
    public async Task GetOrderByIdAsync_ShouldMapOrderToOrderResponse()
    {
        // Arrange
        var repoMock = new Mock<IOrderRepository>();

        var order = new Order
        {
            Id = 101,
            UserId = 7,
            Status = SalesService.Domain.Enums.OrderStatus.Pending.ToString(),
            TotalAmount = 50m,
            Notes = "Test",
            Items = new List<OrderItem>
            {
                new OrderItem { Id = 1, ProductId = 5, ProductName = "P1", UnitPrice = 25m, Quantity = 2, TotalPrice = 50m }
            }
        };

        repoMock.Setup(r => r.GetByIdAndUserIdAsync(101, 7)).ReturnsAsync(order);

        var mapper = SalesService.UnitTests.TestHelpers.TestMapperFactory.CreateMapper();

        var service = new OrderQueryService(repoMock.Object, mapper);

        // Act
        var result = await service.GetOrderByIdAsync(101, 7);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(101);
        result.UserId.Should().Be(7);
        result.Items.Should().HaveCount(1);
        var item = result.Items[0];
        item.ProductId.Should().Be(5);
        item.ProductName.Should().Be("P1");
        item.TotalPrice.Should().Be(50m);
    }

    [Fact]
    public async Task GetOrdersByUserIdAsync_ShouldMapList()
    {
        // Arrange
        var repoMock = new Mock<IOrderRepository>();

        var orders = new List<Order>
        {
            new Order { Id = 201, UserId = 8, Status = SalesService.Domain.Enums.OrderStatus.Confirmed.ToString(), TotalAmount = 10m, Items = new List<OrderItem>() },
            new Order { Id = 202, UserId = 8, Status = SalesService.Domain.Enums.OrderStatus.Cancelled.ToString(), TotalAmount = 0m, Items = new List<OrderItem>() }
        };

        repoMock.Setup(r => r.GetByUserIdAsync(8)).ReturnsAsync(orders);

        var mapper = SalesService.UnitTests.TestHelpers.TestMapperFactory.CreateMapper();

        var service = new OrderQueryService(repoMock.Object, mapper);

        // Act
        var result = await service.GetOrdersByUserIdAsync(8);

        // Assert
        result.Orders.Should().HaveCount(2);
        result.Orders[0].Id.Should().Be(201);
        result.Orders[1].Id.Should().Be(202);
    }
}
