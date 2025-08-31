using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AutoMapper;
using Messaging;
using Messaging.Events;
using SalesService.Data;
using SalesService.Domain.Entities;
using SalesService.Services;

namespace SalesService.UnitTests;

public class ReservationResultProcessorTests
{
    [Fact]
    public async Task ProcessAsync_WhenFailure_ShouldCancelOrderAndPublishEvent()
    {
        // Arrange DI with InMemory DB using TestServiceProviderFactory
        var dbName = "resv_proc_db_" + Guid.NewGuid();
        var (sp, publisherMock) = SalesService.UnitTests.TestHelpers.TestServiceProviderFactory.Create(dbName);

        // Seed order
        using (var scope = sp.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<SalesDbContext>();
            ctx.Orders.Add(new Order { Id = 5001, UserId = 77, Status = SalesService.Domain.Enums.OrderStatus.Pending.ToString() });
            await ctx.SaveChangesAsync();
        }

        // Create a SalesDbContext instance that points to the same in-memory database name
        var options = new DbContextOptionsBuilder<SalesDbContext>().UseInMemoryDatabase(dbName).Options;
        var ctx2 = new SalesDbContext(options);
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<ReservationResultProcessor>();
        var publisher = sp.GetRequiredService<IMessagePublisher>();
        var mapper = sp.GetRequiredService<AutoMapper.IMapper>();

        var processor = new ReservationResultProcessor(ctx2, publisher, mapper, logger);

        var existsBefore = await ctx2.Orders.AnyAsync(o => o.Id == 5001);
        Assert.True(existsBefore, "Seeded order not found in the same DbContext before processing");

        var evt = new OrderReservationCompletedEvent { OrderId = 5001, Success = false, Reason = "no stock" };

        // Act
        await processor.ProcessAsync(evt);

        // Assert - use the same context instance passed to the processor
        var updated = await ctx2.Orders.FindAsync(5001);
        Assert.NotNull(updated);
        Assert.Equal(SalesService.Domain.Enums.OrderStatus.Cancelled.ToString(), updated!.Status);

        publisherMock.Verify(p => p.PublishAsync(It.Is<OrderCancelledEvent>(e => e.OrderId == 5001 && e.UserId == 77)), Times.Once);
    }
}
