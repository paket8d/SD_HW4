// Moved to shared Contracts project
namespace PaymentsService.Messaging;

public class OrderCreated
{
    public Guid OrderId { get; init; }
    public string UserId { get; init; } = string.Empty;
    public decimal Amount { get; init; }
}

public class PaymentStatusChanged
{
    public Guid OrderId { get; init; }
    public string Status { get; init; } = string.Empty; // Paid | Failed
}
