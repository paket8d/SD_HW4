namespace OrdersService;

public enum OrderStatus
{
    Pending,
    Paid,
    Failed
}

public class Order
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = null!;
    public decimal Amount { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class OutboxMessage
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public bool Published { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class InboxMessage
{
    public Guid Id { get; set; }
    public string MessageId { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; }
}

public class CreateOrderRequest
{
    public string UserId { get; set; } = null!;
    public decimal Amount { get; set; }
}
