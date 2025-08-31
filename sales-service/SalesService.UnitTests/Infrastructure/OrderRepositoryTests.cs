using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SalesService.Data;
using SalesService.Domain.Entities;
using SalesService.Infrastructure.Repositories;

namespace SalesService.UnitTests.Infrastructure;

public class OrderRepositoryTests
{
    private static SalesDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<SalesDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new SalesDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_ShouldPersistOrder()
    {
        var dbName = "or_repo_create_" + Guid.NewGuid();
        using var ctx = CreateContext(dbName);
        var repo = new OrderRepository(ctx);

        var order = new Order { Id = 10, UserId = 3, Status = "Pending", TotalAmount = 123m };
        var created = await repo.CreateAsync(order);

        created.Should().NotBeNull();
        created.Id.Should().Be(10);

        var fetched = await ctx.Orders.FindAsync(10);
        fetched.Should().NotBeNull();
        fetched!.UserId.Should().Be(3);
    }

    [Fact]
    public async Task GetByIdAsync_And_GetByIdAndUserIdAsync_ReturnsExpected()
    {
        var dbName = "or_repo_get_" + Guid.NewGuid();
        using (var ctx = CreateContext(dbName))
        {
            ctx.Orders.Add(new Order { Id = 20, UserId = 5, Status = "Pending" });
            ctx.Orders.Add(new Order { Id = 21, UserId = 6, Status = "Pending" });
            await ctx.SaveChangesAsync();
        }

        using (var ctx = CreateContext(dbName))
        {
            var repo = new OrderRepository(ctx);
            var byId = await repo.GetByIdAsync(20);
            byId.Should().NotBeNull();
            byId!.UserId.Should().Be(5);

            var byIdUser = await repo.GetByIdAndUserIdAsync(20, 5);
            byIdUser.Should().NotBeNull();

            var notFound = await repo.GetByIdAndUserIdAsync(20, 999);
            notFound.Should().BeNull();
        }
    }

    [Fact]
    public async Task GetByUserIdAsync_ReturnsOrdersOrderedByCreatedAtDesc()
    {
        var dbName = "or_repo_list_" + Guid.NewGuid();
        using (var ctx = CreateContext(dbName))
        {
            ctx.Orders.Add(new Order { Id = 30, UserId = 7, CreatedAt = DateTime.UtcNow.AddHours(-1) });
            ctx.Orders.Add(new Order { Id = 31, UserId = 7, CreatedAt = DateTime.UtcNow });
            ctx.Orders.Add(new Order { Id = 32, UserId = 8, CreatedAt = DateTime.UtcNow });
            await ctx.SaveChangesAsync();
        }

        using (var ctx = CreateContext(dbName))
        {
            var repo = new OrderRepository(ctx);
            var list = (await repo.GetByUserIdAsync(7)).ToList();
            list.Count.Should().Be(2);
            list.First().Id.Should().Be(31); // newest first
        }
    }

    [Fact]
    public async Task UpdateAsync_ShouldSetUpdatedAt()
    {
        var dbName = "or_repo_update_" + Guid.NewGuid();
        using (var ctx = CreateContext(dbName))
        {
            ctx.Orders.Add(new Order { Id = 40, UserId = 9, Status = "Pending" });
            await ctx.SaveChangesAsync();
        }

        using (var ctx = CreateContext(dbName))
        {
            var repo = new OrderRepository(ctx);
            var order = await repo.GetByIdAsync(40);
            order.Should().NotBeNull();
            order!.Status = "Reserved";
            await repo.UpdateAsync(order);
        }

        using (var ctx = CreateContext(dbName))
        {
            var updated = await ctx.Orders.FindAsync(40);
            updated.Should().NotBeNull();
            updated!.UpdatedAt.Should().NotBeNull();
            updated.Status.Should().Be("Reserved");
        }
    }

    [Fact]
    public async Task DeleteAsync_RemovesOrderIfExists()
    {
        var dbName = "or_repo_delete_" + Guid.NewGuid();
        using (var ctx = CreateContext(dbName))
        {
            ctx.Orders.Add(new Order { Id = 50, UserId = 10 });
            await ctx.SaveChangesAsync();
        }

        using (var ctx = CreateContext(dbName))
        {
            var repo = new OrderRepository(ctx);
            await repo.DeleteAsync(50);
        }

        using (var ctx = CreateContext(dbName))
        {
            var shouldBeNull = await ctx.Orders.FindAsync(50);
            shouldBeNull.Should().BeNull();
        }
    }
}
