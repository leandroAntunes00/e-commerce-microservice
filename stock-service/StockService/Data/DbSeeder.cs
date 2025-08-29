using Microsoft.Extensions.DependencyInjection;
using StockService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace StockService.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StockDbContext>();

        if (await db.Products.AnyAsync())
        {
            return;
        }

        db.Products.AddRange(
            new Product
            {
                Id = 1,
                Name = "Smartphone Samsung Galaxy S23",
                Description = "Smartphone de última geração com câmera de alta resolução",
                Price = 2999.99m,
                Category = "Eletrônicos",
                StockQuantity = 50,
                ImageUrl = "https://example.com/images/galaxy-s23.jpg",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Product
            {
                Id = 2,
                Name = "Notebook Dell Inspiron 15",
                Description = "Notebook para trabalho e estudos com processador Intel i5",
                Price = 3999.99m,
                Category = "Informática",
                StockQuantity = 30,
                ImageUrl = "https://example.com/images/dell-inspiron.jpg",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Product
            {
                Id = 3,
                Name = "Fone de Ouvido Bluetooth Sony",
                Description = "Fone de ouvido wireless com cancelamento de ruído ativo",
                Price = 599.99m,
                Category = "Áudio",
                StockQuantity = 100,
                ImageUrl = "https://example.com/images/sony-headphones.jpg",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        );

        await db.SaveChangesAsync();
    }
}
