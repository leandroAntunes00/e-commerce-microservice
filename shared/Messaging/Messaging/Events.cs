using System.Text.Json.Serialization;

namespace Messaging.Events;

// Evento disparado quando um pedido é criado
public class OrderCreatedEvent : BaseEvent
{
    [JsonIgnore]
    public override string EventType => "OrderCreated";

    public int OrderId { get; set; }
    public int UserId { get; set; }
    public List<OrderItemEvent> Items { get; set; } = new List<OrderItemEvent>();
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class OrderItemEvent
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

// Evento disparado quando um pedido é cancelado
public class OrderCancelledEvent : BaseEvent
{
    [JsonIgnore]
    public override string EventType => "OrderCancelled";

    public int OrderId { get; set; }
    public int UserId { get; set; }
    public List<OrderItemEvent> Items { get; set; } = new List<OrderItemEvent>();
    public DateTime CancelledAt { get; set; }
}

// Evento disparado quando o estoque de um produto é atualizado
public class StockUpdatedEvent : BaseEvent
{
    [JsonIgnore]
    public override string EventType => "StockUpdated";

    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int PreviousStock { get; set; }
    public int NewStock { get; set; }
    public string Operation { get; set; } = string.Empty; // "Reserved", "Released", "Updated"
    public DateTime UpdatedAt { get; set; }
}

// Evento disparado quando a tentativa de reserva de um pedido foi concluída (sucesso ou falha)
public class OrderReservationCompletedEvent : BaseEvent
{
    [JsonIgnore]
    public override string EventType => "OrderReservationCompleted";

    public int OrderId { get; set; }
    public bool Success { get; set; }
    public string? Reason { get; set; }
    public DateTime OccurredAt { get; set; }
}

// Evento disparado quando um produto é criado
public class ProductCreatedEvent : BaseEvent
{
    [JsonIgnore]
    public override string EventType => "ProductCreated";

    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string Category { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
