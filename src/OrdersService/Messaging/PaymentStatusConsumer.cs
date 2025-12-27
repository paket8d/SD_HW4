using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrdersService.Data;
using Contracts;

namespace OrdersService.Messaging;

public class PaymentStatusConsumer : IConsumer<PaymentStatusChanged>
{
    private readonly OrdersDbContext _db;

    public PaymentStatusConsumer(OrdersDbContext db)
    {
        _db = db;
    }

    public async Task Consume(ConsumeContext<PaymentStatusChanged> context)
    {
        var msgId = context.MessageId?.ToString();
        if (msgId == null) return;

        if (await _db.Inbox.AnyAsync(i => i.MessageId == msgId))
            return;

        var msg = context.Message;
        var inbox = new InboxMessage
        {
            Id = Guid.NewGuid(),
            MessageId = msgId,
            Payload = JsonSerializer.Serialize(msg),
            ReceivedAt = DateTime.UtcNow
        };

        var order = await _db.Orders.FindAsync(msg.OrderId);
        if (order == null)
        {
            _db.Inbox.Add(inbox);
            await _db.SaveChangesAsync();
            return;
        }

        order.Status = msg.Status.Equals("Paid", StringComparison.OrdinalIgnoreCase)
            ? OrderStatus.Paid
            : OrderStatus.Failed;

        _db.Inbox.Add(inbox);
        await _db.SaveChangesAsync();
    }
}
