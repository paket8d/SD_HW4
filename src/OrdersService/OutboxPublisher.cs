﻿using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using MassTransit;
using OrdersService.Data;
using OrdersService.Messaging;
using ContractOrderCreated = Contracts.OrderCreated;

namespace OrdersService;

public class OutboxPublisher : BackgroundService
{
    private readonly IServiceProvider _provider;
    private readonly int _intervalMs;

    public OutboxPublisher(IServiceProvider provider, IConfiguration configuration)
    {
        _provider = provider;
        _intervalMs = configuration.GetValue<int?>("Outbox:IntervalMs") ?? 1000;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishBatch(stoppingToken);
            }
            catch
            {
            }

            await Task.Delay(_intervalMs, stoppingToken);
        }
    }

    private async Task PublishBatch(CancellationToken ct)
    {
        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var messages = await db.Outbox.Where(o => !o.Published).OrderBy(o => o.CreatedAt).Take(50).ToListAsync(ct);
        foreach (var msg in messages)
        {
            object? typedPayload = null;
            if (msg.EventType == nameof(ContractOrderCreated))
            {
                typedPayload = JsonSerializer.Deserialize<ContractOrderCreated>(msg.Payload);
            }

            if (typedPayload != null)
            {
                await publisher.Publish(typedPayload, ctx => ctx.MessageId = msg.Id);
                msg.Published = true;
            }
        }

        if (messages.Count > 0)
            await db.SaveChangesAsync(ct);
    }
}
