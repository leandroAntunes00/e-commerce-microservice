using StockService.Domain.Entities;

namespace StockService.Application.DTOs;

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public static ProductDto FromEntity(Product p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Description = p.Description,
        Price = p.Price,
        Category = p.Category,
        StockQuantity = p.StockQuantity,
        ImageUrl = p.ImageUrl,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt
    };
}
