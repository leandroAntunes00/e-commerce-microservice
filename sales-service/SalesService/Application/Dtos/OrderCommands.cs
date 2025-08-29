namespace SalesService.Application.Dtos;

public class CreateOrderCommand
{
    public int UserId { get; set; }
    public List<OrderItemCommand> Items { get; set; } = new List<OrderItemCommand>();
    public string? Notes { get; set; }
}

public class OrderItemCommand
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

public class ProcessPaymentCommand
{
    public int UserId { get; set; }
    public int OrderId { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
}

public class CancelOrderCommand
{
    public int UserId { get; set; }
    public int OrderId { get; set; }
}
