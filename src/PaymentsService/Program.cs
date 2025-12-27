using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using PaymentsService;
using PaymentsService.Data;
using PaymentsService.Messaging;
using Contracts;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default") ?? "Host=localhost;Port=5432;Database=sdhw4;Username=sdhw4;Password=sdhw4";
var rabbitHost = builder.Configuration["Rabbit:Host"] ?? "rabbitmq";
var rabbitUser = builder.Configuration["Rabbit:User"] ?? "guest";
var rabbitPass = builder.Configuration["Rabbit:Password"] ?? "guest";


builder.Services.AddDbContext<PaymentsDbContext>(opt => opt.UseNpgsql(connectionString));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderCreatedConsumer>();
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitHost, "/", h =>
        {
            h.Username(rabbitUser);
            h.Password(rabbitPass);
        });
        cfg.ReceiveEndpoint("payments-order-created-v2", e =>
        {
            e.ConfigureConsumeTopology = false;
            e.Bind<OrderCreated>();
            e.ConfigureConsumer<OrderCreatedConsumer>(context);
        });
    });
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});


builder.Services.AddHostedService<OutboxPublisher>();

var app = builder.Build();
app.Use(async (ctx, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        Console.WriteLine("UNHANDLED EXCEPTION:");
        Console.WriteLine(ex.ToString());
        throw;
    }
});

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
}


await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
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

app.MapPost("/accounts", async (CreateAccountRequest req, PaymentsDbContext db) =>
{
    if (await db.Accounts.AnyAsync(a => a.UserId == req.UserId))
        return Results.Conflict("Account already exists");
    var acc = new Account { UserId = req.UserId, Balance = 0m, Version = 0 };
    db.Accounts.Add(acc);
    await db.SaveChangesAsync();
    return Results.Created($"/accounts/{acc.UserId}", new
    {
        acc.UserId,
        acc.Balance,
        acc.Version
    });
});

app.MapPost("/accounts/top-up", async (TopUpRequest req, PaymentsDbContext db) =>
{
    var acc = await db.Accounts.FirstOrDefaultAsync(a => a.UserId == req.UserId);
    if (acc == null) return Results.NotFound();
    acc.Balance += req.Amount;
    acc.Version++;
    await db.SaveChangesAsync();
    return Results.Ok(acc);
});

app.MapGet("/accounts", async (string userId, PaymentsDbContext db) =>
{
    var acc = await db.Accounts.FirstOrDefaultAsync(a => a.UserId == userId);
    return acc is null ? Results.NotFound() : Results.Ok(acc);
});

app.Run();
