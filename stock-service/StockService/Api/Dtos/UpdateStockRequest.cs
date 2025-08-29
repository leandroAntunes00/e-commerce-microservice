using System.ComponentModel.DataAnnotations;

namespace StockService.Api.Dtos;

public class UpdateStockRequest
{
    [Required]
    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }
}
