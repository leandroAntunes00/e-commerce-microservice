using System.ComponentModel.DataAnnotations;

namespace StockService.Api.Dtos;

public class ReserveStockRequest
{
    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}

public class ReleaseStockRequest
{
    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}
