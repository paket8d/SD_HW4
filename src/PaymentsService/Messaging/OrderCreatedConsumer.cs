﻿using System;
using System.Text.Json;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using PaymentsService.Data;
using Contracts;

namespace PaymentsService.Messaging;

public class OrderCreatedConsumer : IConsumer<OrderCreated>
{
    private readonly PaymentsDbContext _db;

    public OrderCreatedConsumer(PaymentsDbContext db)
    {
        _db = db;
    }

    public async Task Consume(ConsumeContext<OrderCreated> context)
    {
        var msgId = context.MessageId?.ToString();
        if (msgId == null) return;

        if (await _db.Inbox.AnyAsync(i => i.MessageId == msgId))
            return;

        var msg = context.Message;

        var existingTransaction = await _db.Transactions.FirstOrDefaultAsync(t => t.OrderId == msg.OrderId);
        if (existingTransaction != null)
        {
            var inbox = new InboxMessage
            {
                Id = Guid.NewGuid(),
                MessageId = msgId,
                Payload = JsonSerializer.Serialize(msg),
                ReceivedAt = DateTime.UtcNow
            };
            _db.Inbox.Add(inbox);
            await _db.SaveChangesAsync();
            return;
        }

        var inboxEntry = new InboxMessage
        {
            Id = Guid.NewGuid(),
            MessageId = msgId,
            Payload = JsonSerializer.Serialize(msg),
            ReceivedAt = DateTime.UtcNow
        };

        var tx = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            OrderId = msg.OrderId,
            UserId = msg.UserId,
            Amount = msg.Amount,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var account = await _db.Accounts.FirstOrDefaultAsync(a => a.UserId == msg.UserId);
            if (account == null)
            {
                tx.Status = "Failed";
            }
            else if (account.Balance >= msg.Amount)
            {
                account.Balance -= msg.Amount;
                account.Version++;
                tx.Status = "Paid";
                _db.Accounts.Update(account);
            }
            else
            {
                tx.Status = "Failed";
            }

            var statusEvent = new PaymentStatusChanged
            {
                OrderId = msg.OrderId,
                Status = tx.Status
            };

            var outbox = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                EventType = nameof(PaymentStatusChanged),
                Payload = JsonSerializer.Serialize(statusEvent),
                Published = false,
                CreatedAt = DateTime.UtcNow
            };

            _db.Inbox.Add(inboxEntry);
            _db.Transactions.Add(tx);
            _db.Outbox.Add(outbox);

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
