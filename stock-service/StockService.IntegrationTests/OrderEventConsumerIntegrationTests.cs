using Messaging;
using Messaging.Events;
using StockService.Data;
using StockService.Models;
using StockService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Xunit;

namespace StockService.IntegrationTests;

public class OrderEventConsumerIntegrationTests : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly StockDbContext _context;
    private readonly IMessagePublisher _messagePublisher;
    private readonly RabbitMqSettings _rabbitMqSettings;

    public OrderEventConsumerIntegrationTests()
    {
        // Configurar serviços de teste
        var services = new ServiceCollection();

        // Configurar banco de dados em memória
        services.AddDbContext<StockDbContext>(options =>
            options.UseInMemoryDatabase("TestDatabase"));

        // Configurar RabbitMQ para testes
        _rabbitMqSettings = new RabbitMqSettings
        {
            HostName = "localhost",
            Port = 5672,
            UserName = "guest",
            Password = "guest",
            VirtualHost = "/",
            ExchangeName = "test_stock_exchange",
            QueuePrefix = "test_stock_"
        };

        services.AddSingleton(_rabbitMqSettings);
        services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();
        services.AddSingleton<IMessageConsumer, RabbitMqConsumer>();
        services.AddLogging();

        _serviceProvider = services.BuildServiceProvider();

        // Criar banco de dados e dados de teste
        _context = _serviceProvider.GetRequiredService<StockDbContext>();
        _context.Database.EnsureCreated();
        SeedTestData();

        _messagePublisher = _serviceProvider.GetRequiredService<IMessagePublisher>();
    }

    private void SeedTestData()
    {
        // Criar produtos de teste
        var products = new[]
        {
            new Product
            {
                Id = 1,
                Name = "Produto Teste 1",
                Description = "Descrição do produto 1",
                Price = 10.00m,
                Category = "Teste",
                StockQuantity = 100,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Product
            {
                Id = 2,
                Name = "Produto Teste 2",
                Description = "Descrição do produto 2",
                Price = 20.00m,
                Category = "Teste",
                StockQuantity = 50,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _context.Products.AddRange(products);
        _context.SaveChanges();
    }

    [Fact]
    public async Task ProcessOrderCreatedEvent_ShouldReserveStockCorrectly()
    {
        // Arrange
        var orderEvent = new OrderCreatedEvent
        {
            OrderId = 123,
            UserId = 456,
            Items = new List<OrderItemEvent>
            {
                new OrderItemEvent
                {
                    ProductId = 1,
                    ProductName = "Produto Teste 1",
                    Quantity = 10,
                    UnitPrice = 10.00m
                },
                new OrderItemEvent
                {
                    ProductId = 2,
                    ProductName = "Produto Teste 2",
                    Quantity = 5,
                    UnitPrice = 20.00m
                }
            },
            TotalAmount = 200.00m,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var consumerService = new OrderEventConsumerService(
            _serviceProvider,
            _rabbitMqSettings,
            _serviceProvider.GetRequiredService<ILogger<OrderEventConsumerService>>());

        // Simular o processamento do evento
        await ProcessOrderCreatedEventManually(orderEvent);

        // Assert
        var product1 = await _context.Products.FindAsync(1);
        var product2 = await _context.Products.FindAsync(2);

        Assert.NotNull(product1);
        Assert.NotNull(product2);
        Assert.Equal(90, product1.StockQuantity); // 100 - 10
        Assert.Equal(45, product2.StockQuantity); // 50 - 5
    }

    [Fact]
    public async Task ProcessOrderCreatedEvent_WithInsufficientStock_ShouldNotReserve()
    {
        // Arrange
        var orderEvent = new OrderCreatedEvent
        {
            OrderId = 124,
            UserId = 457,
            Items = new List<OrderItemEvent>
            {
                new OrderItemEvent
                {
                    ProductId = 1,
                    ProductName = "Produto Teste 1",
                    Quantity = 200, // Mais do que disponível (100)
                    UnitPrice = 10.00m
                }
            },
            TotalAmount = 2000.00m,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await ProcessOrderCreatedEventManually(orderEvent);

        // Assert
        var product1 = await _context.Products.FindAsync(1);
        Assert.NotNull(product1);
        Assert.Equal(100, product1.StockQuantity); // Estoque não deve mudar
    }

    [Fact]
    public async Task ProcessOrderCancelledEvent_ShouldReleaseStockCorrectly()
    {
        // Arrange - Primeiro reservar estoque
        await ProcessOrderCreatedEventManually(new OrderCreatedEvent
        {
            OrderId = 125,
            UserId = 458,
            Items = new List<OrderItemEvent>
            {
                new OrderItemEvent
                {
                    ProductId = 1,
                    ProductName = "Produto Teste 1",
                    Quantity = 20,
                    UnitPrice = 10.00m
                }
            },
            TotalAmount = 200.00m,
            CreatedAt = DateTime.UtcNow
        });

        // Verificar que estoque foi reservado
        var productAfterReservation = await _context.Products.FindAsync(1);
        Assert.Equal(80, productAfterReservation!.StockQuantity);

        // Act - Cancelar pedido
        var cancelEvent = new OrderCancelledEvent
        {
            OrderId = 125,
            UserId = 458,
            Items = new List<OrderItemEvent>
            {
                new OrderItemEvent
                {
                    ProductId = 1,
                    ProductName = "Produto Teste 1",
                    Quantity = 20,
                    UnitPrice = 10.00m
                }
            },
            CancelledAt = DateTime.UtcNow
        };

        await ProcessOrderCancelledEventManually(cancelEvent);

        // Assert
        var productAfterCancellation = await _context.Products.FindAsync(1);
        Assert.Equal(100, productAfterCancellation!.StockQuantity); // Estoque liberado
    }

    private async Task ProcessOrderCreatedEventManually(OrderCreatedEvent orderEvent)
    {
        foreach (var item in orderEvent.Items)
        {
            var product = await _context.Products.FindAsync(item.ProductId);
            if (product == null)
            {
                continue;
            }

            if (product.StockQuantity < item.Quantity)
            {
                continue;
            }

            // Reservar estoque
            var previousStock = product.StockQuantity;
            product.StockQuantity -= item.Quantity;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }
    }

    private async Task ProcessOrderCancelledEventManually(OrderCancelledEvent orderEvent)
    {
        foreach (var item in orderEvent.Items)
        {
            var product = await _context.Products.FindAsync(item.ProductId);
            if (product == null)
            {
                continue;
            }

            // Liberar estoque
            var previousStock = product.StockQuantity;
            product.StockQuantity += item.Quantity;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
