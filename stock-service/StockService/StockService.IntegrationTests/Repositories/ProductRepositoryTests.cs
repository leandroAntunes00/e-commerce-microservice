using System;
using Xunit;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StockService.Data;
using StockService.Domain.Entities;
using StockService.Infrastructure.Repositories;
using System.Threading.Tasks;

namespace StockService.IntegrationTests.Repositories;

public class ProductRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly StockDbContext _context;
    private readonly ProductRepository _repository;

    public ProductRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<StockDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new StockDbContext(options);
        _context.Database.EnsureCreated();

        _repository = new ProductRepository(_context);
    }

    [Fact]
    public async Task CreateAndGetById_Works()
    {
        var p = new Product { Name = "T", Price = 10m, Category = "C", StockQuantity = 5 };
        var created = await _repository.CreateAsync(p);

        created.Id.Should().BeGreaterThan(0);

        var fetched = await _repository.GetByIdAsync(created.Id);
        fetched.Should().NotBeNull();
        fetched!.Name.Should().Be("T");
    }

    [Fact]
    public async Task GetAllActive_ReturnsActive()
    {
        var p1 = new Product { Name = "A", Price = 1m, Category = "X", StockQuantity = 1 };
        var p2 = new Product { Name = "B", Price = 2m, Category = "X", StockQuantity = 2, IsActive = false };
        await _repository.CreateAsync(p1);
        await _repository.CreateAsync(p2);

        var list = await _repository.GetAllActiveAsync();
        list.Should().ContainSingle(x => x.Name == "A");
        list.Should().NotContain(x => x.Name == "B");
    }

    [Fact]
    public async Task UpdateAndExists_Search_Works()
    {
        var p = new Product { Name = "SearchMe", Price = 5m, Category = "Cats", StockQuantity = 2 };
        var created = await _repository.CreateAsync(p);

        created.StockQuantity = 10;
        await _repository.UpdateAsync(created);

        var exists = await _repository.ExistsAsync(created.Id);
        exists.Should().BeTrue();

        var results = await _repository.SearchAsync("SearchMe");
        results.Should().ContainSingle(r => r.Id == created.Id);
    }

    [Fact]
    public async Task Delete_MarksInactive()
    {
        var p = new Product { Name = "ToDelete", Price = 1m, Category = "C", StockQuantity = 1 };
        var created = await _repository.CreateAsync(p);

        await _repository.DeleteAsync(created.Id);

        var fetched = await _repository.GetByIdAsync(created.Id);
        fetched.Should().NotBeNull();
        fetched!.IsActive.Should().BeFalse();

        var exists = await _repository.ExistsAsync(created.Id);
        exists.Should().BeFalse();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Close();
        _connection.Dispose();
    }
}
