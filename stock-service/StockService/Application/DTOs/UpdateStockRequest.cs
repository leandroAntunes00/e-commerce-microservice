using System.ComponentModel.DataAnnotations;

namespace StockService.Application.DTOs;

public class UpdateStockRequest
{
    [Required]
    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }
}
