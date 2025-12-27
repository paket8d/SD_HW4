using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrdersService;
using OrdersService.Data;
using OrdersService.Messaging;
using Contracts;
using ContractOrderCreated = Contracts.OrderCreated;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default") ?? "Host=localhost;Port=5432;Database=sdhw4;Username=sdhw4;Password=sdhw4";
var rabbitHost = builder.Configuration.GetValue<string>("Rabbit__Host") ?? "rabbitmq";
var rabbitUser = builder.Configuration.GetValue<string>("Rabbit__User") ?? "guest";
var rabbitPass = builder.Configuration.GetValue<string>("Rabbit__Password") ?? "guest";

builder.Services.AddDbContext<OrdersDbContext>(opt => opt.UseNpgsql(connectionString));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<PaymentStatusConsumer>();
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitHost, "/", h =>
        {
            h.Username(rabbitUser);
            h.Password(rabbitPass);
        });
        cfg.ReceiveEndpoint("orders-payment-status", e =>
        {
            e.ConfigureConsumer<PaymentStatusConsumer>(context);
        });
    });
});

builder.Services.AddHostedService<OutboxPublisher>();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
    var retries = 10;
    while (retries-- > 0)
    {
        try
        {
            await db.Database.MigrateAsync();
            break;
        }
        catch
        {
            if (retries == 0) throw;
            await Task.Delay(2000);
        }
    }
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("/orders", async (CreateOrderRequest req, OrdersDbContext db) =>
{
    var order = new Order
    {
        Id = Guid.NewGuid(),
        UserId = req.UserId,
        Amount = req.Amount,
        Status = OrderStatus.Pending,
        CreatedAt = DateTime.UtcNow
    };

    var outboxEntry = new OutboxMessage
    {
        Id = Guid.NewGuid(),
        EventType = nameof(ContractOrderCreated),
        Payload = JsonSerializer.Serialize(new ContractOrderCreated
        {
            OrderId = order.Id,
            UserId = order.UserId,
            Amount = order.Amount
        }),
        Published = false,
        CreatedAt = DateTime.UtcNow
    };

    await using var transaction = await db.Database.BeginTransactionAsync();
    try
    {
        db.Orders.Add(order);
        db.Outbox.Add(outboxEntry);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }

    return Results.Created($"/orders/{order.Id}", order);
});

app.MapGet("/orders", async (string userId, OrdersDbContext db) =>
{
    var orders = await db.Orders.Where(o => o.UserId == userId).OrderByDescending(o => o.CreatedAt).ToListAsync();
    return Results.Ok(orders);
});

app.MapGet("/orders/{id:guid}", async (Guid id, OrdersDbContext db) =>
{
    var order = await db.Orders.FindAsync(id);
    return order is null ? Results.NotFound() : Results.Ok(order);
});

app.Run();
