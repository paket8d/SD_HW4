using System.ComponentModel.DataAnnotations;

namespace PaymentsService;

public class Account
{
    [Key]
    public string UserId { get; set; } = null!;
    public decimal Balance { get; set; }
    [ConcurrencyCheck]
    public int Version { get; set; }
}

public class PaymentTransaction
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string UserId { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Pending"; // Pending | Paid | Failed
    public DateTime CreatedAt { get; set; }
}

public class InboxMessage
{
    public Guid Id { get; set; }
    public string MessageId { get; set; } = null!;
    public string Payload { get; set; } = null!;
    public DateTime ReceivedAt { get; set; }
}

public class OutboxMessage
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = null!;
    public string Payload { get; set; } = null!;
    public bool Published { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PaymentStatusChangedEvent
{
    public Guid OrderId { get; set; }
    public string Status { get; set; } = null!; // Paid | Failed
}

public class CreateAccountRequest
{
    public string UserId { get; set; } = null!;
}

public class TopUpRequest
{
    public string UserId { get; set; } = null!;
    public decimal Amount { get; set; }
}
