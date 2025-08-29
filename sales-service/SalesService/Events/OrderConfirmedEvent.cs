namespace Messaging.Events;

public class OrderConfirmedEvent : Messaging.BaseEvent
{
    public override string EventType => "OrderConfirmed";
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public DateTime ConfirmedAt { get; set; }
    public string Method { get; set; } = string.Empty; // Card, Pix, Boleto
}
