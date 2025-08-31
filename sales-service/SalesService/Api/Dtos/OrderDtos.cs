using System.ComponentModel.DataAnnotations;

namespace SalesService.Api.Dtos;

public class CreateOrderRequest
{
    [Required]
    [MinLength(1, ErrorMessage = "At least one item is required")]
    public List<OrderItemRequest> Items { get; set; } = new List<OrderItemRequest>();

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class OrderItemRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "ProductId must be greater than 0")]
    public int ProductId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; }
}

public class PaymentRequest
{
    [Range(1, int.MaxValue)]
    public int OrderId { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }
}

public class ConfirmPaymentRequest
{
    [Range(1, int.MaxValue)]
    public int OrderId { get; set; }

    [Required]
    public string Method { get; set; } = string.Empty;
}

public class OrderResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<OrderItemResponse> Items { get; set; } = new List<OrderItemResponse>();
}

public class OrderItemResponse
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class OrdersListResponse
{
    public List<OrderResponse> Orders { get; set; } = new List<OrderResponse>();
}
