using System.ComponentModel.DataAnnotations;

namespace StockService.Api.Dtos;

public class CreateProductRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Price { get; set; }

    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    [Required]
    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

    [MaxLength(500)]
    public string ImageUrl { get; set; } = string.Empty;
}
