
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options => options.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var ordersBase = builder.Configuration.GetValue<string>("Services:Orders") ?? "http://orders-service:5001";
var paymentsBase = builder.Configuration.GetValue<string>("Services:Payments") ?? "http://payments-service:5002";

var httpClient = new HttpClient();

var app = builder.Build();

app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("/accounts", async (CreateAccountRequest req, HttpContext ctx) =>
{
    await ProxyWithBody(ctx, paymentsBase, req, httpClient);
})
.WithName("CreateAccount")
.WithOpenApi();

app.MapPost("/accounts/top-up", async (TopUpRequest req, HttpContext ctx) =>
{
    await ProxyWithBody(ctx, paymentsBase, req, httpClient);
})
.WithName("TopUpAccount")
.WithOpenApi();

app.MapGet("/accounts", async (string userId, HttpContext ctx) =>
{
    await Proxy(ctx, paymentsBase, httpClient);
})
.WithName("GetAccount")
.WithOpenApi();

app.MapPost("/orders", async (CreateOrderRequest req, HttpContext ctx) =>
{
    await ProxyWithBody(ctx, ordersBase, req, httpClient);
})
.WithName("CreateOrder")
.WithOpenApi();

app.MapGet("/orders/{id:guid}", async (Guid id, HttpContext ctx) =>
{
    await Proxy(ctx, ordersBase, httpClient);
})
.WithName("GetOrderById")
.WithOpenApi();

app.MapGet("/orders", async (string userId, HttpContext ctx) =>
{
    await Proxy(ctx, ordersBase, httpClient);
})
.WithName("GetOrdersByUser")
.WithOpenApi();

app.Map("/swagger/orders/{*path}", async context =>
{
    await ProxyWithRewrite(context, ordersBase, "/swagger/orders", "/swagger", httpClient);
});
app.Map("/swagger/payments/{*path}", async context =>
{
    await ProxyWithRewrite(context, paymentsBase, "/swagger/payments", "/swagger", httpClient);
});

app.Run();

static async Task ProxyWithRewrite(HttpContext context, string targetBase, string fromPrefix, string toPrefix, HttpClient client)
{
    var rewrittenPath = context.Request.Path.ToString().Replace(fromPrefix, toPrefix, StringComparison.OrdinalIgnoreCase);
    var query = context.Request.QueryString.ToString();
    var target = targetBase.TrimEnd('/') + rewrittenPath + query;
    await Forward(context, target, client);
}

static async Task Proxy(HttpContext context, string targetBase, HttpClient client)
{
    var path = context.Request.Path.ToString();
    var query = context.Request.QueryString.ToString();
    var target = targetBase.TrimEnd('/') + path + query;
    await Forward(context, target, client);
}

static async Task Forward(HttpContext context, string target, HttpClient client)
{
    context.Request.EnableBuffering();
    if (context.Request.Body.CanSeek)
    {
        context.Request.Body.Position = 0;
    }

    using var targetRequest = new HttpRequestMessage(new HttpMethod(context.Request.Method), target);

    if (context.Request.ContentLength > 0 || (context.Request.Body != null && context.Request.Body.CanSeek && context.Request.Body.Length > 0))
    {
        var buffer = new MemoryStream();
        await context.Request.Body.CopyToAsync(buffer);
        buffer.Position = 0;

        var content = new StreamContent(buffer);
        targetRequest.Content = content;

        foreach (var header in context.Request.Headers)
        {
            if (header.Key.StartsWith("Content-", StringComparison.OrdinalIgnoreCase))
            {
                if (!header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                {
                    content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }
        }
    }

    foreach (var header in context.Request.Headers)
    {
        if (header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase) ||
            header.Key.StartsWith("Content-", StringComparison.OrdinalIgnoreCase))
            continue;

        targetRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
    }

    using var response = await client.SendAsync(targetRequest, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
    context.Response.StatusCode = (int)response.StatusCode;

    foreach (var header in response.Headers)
        context.Response.Headers[header.Key] = header.Value.ToArray();
    foreach (var header in response.Content.Headers)
        context.Response.Headers[header.Key] = header.Value.ToArray();
    context.Response.Headers.Remove("transfer-encoding");

    await response.Content.CopyToAsync(context.Response.Body);
}

static async Task ProxyWithBody<T>(HttpContext context, string targetBase, T body, HttpClient client)
{
    var path = context.Request.Path.ToString();
    var query = context.Request.QueryString.ToString();
    var target = targetBase.TrimEnd('/') + path + query;

    using var targetRequest = new HttpRequestMessage(new HttpMethod(context.Request.Method), target);
    
    var json = System.Text.Json.JsonSerializer.Serialize(body);
    targetRequest.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
    
    foreach (var header in context.Request.Headers)
    {
        if (header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase) || 
            header.Key.StartsWith("Content-", StringComparison.OrdinalIgnoreCase))
            continue;
        targetRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
    }
    
    using var response = await client.SendAsync(targetRequest, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
    
    context.Response.StatusCode = (int)response.StatusCode;
    foreach (var header in response.Headers)
        context.Response.Headers[header.Key] = header.Value.ToArray();
    foreach (var header in response.Content.Headers)
        context.Response.Headers[header.Key] = header.Value.ToArray();
    context.Response.Headers.Remove("transfer-encoding");
    await response.Content.CopyToAsync(context.Response.Body);
}

class CreateAccountRequest { public string UserId { get; set; } = string.Empty; }
class TopUpRequest { public string UserId { get; set; } = string.Empty; public decimal Amount { get; set; } }
class CreateOrderRequest { public string UserId { get; set; } = string.Empty; public decimal Amount { get; set; } }
