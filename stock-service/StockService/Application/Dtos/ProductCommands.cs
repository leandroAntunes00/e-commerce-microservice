namespace StockService.Application.Dtos;

public class GetProductsQuery
{
    public string? Category { get; set; }
    public string? SearchTerm { get; set; }
}

public class GetProductQuery
{
    public int ProductId { get; set; }
}

public class CreateProductCommand
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
}

public class UpdateProductCommand
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
}

public class UpdateStockCommand
{
    public int ProductId { get; set; }
    public int NewStockQuantity { get; set; }
}
