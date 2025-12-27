namespace Contracts;

public class OrderCreated
{
    public Guid OrderId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class PaymentStatusChanged
{
    public Guid OrderId { get; set; }
    public string Status { get; set; } = string.Empty; // Paid | Failed
}

