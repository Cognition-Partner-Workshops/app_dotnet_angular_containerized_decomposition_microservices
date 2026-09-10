using Microsoft.EntityFrameworkCore;
using Order.API.Services;
using Order.Domain.Interfaces;
using Order.Infrastructure.Data;
using Order.Infrastructure.Http;
using Order.Infrastructure.Messaging;
using Order.Infrastructure.Orders;
using Order.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsql => npgsql.EnableRetryOnFailure(maxRetryCount: 10, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null)));

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.Configure<GatewayOptions>(builder.Configuration.GetSection(GatewayOptions.SectionName));

builder.Services.AddScoped<IOrdersService, OrdersService>();
builder.Services.AddScoped<OrderPlacementService>();
builder.Services.AddScoped<OrderViewModelMapper>();
builder.Services.AddSingleton<IOrderEventPublisher, RabbitMqOrderEventPublisher>();

builder.Services.AddHttpClient<IOrderEnrichmentClient, GatewayEnrichmentClient>(GatewayEnrichmentClient.HttpClientName, (provider, client) =>
{
    var gateway = builder.Configuration.GetSection(GatewayOptions.SectionName).Get<GatewayOptions>() ?? new GatewayOptions();
    client.BaseAddress = new Uri(gateway.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(gateway.TimeoutSeconds);
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    for (var attempt = 1; ; attempt++)
    {
        try
        {
            db.Database.Migrate();
            break;
        }
        catch (Exception ex) when (attempt < 10)
        {
            logger.LogWarning(ex, "Database not reachable yet (attempt {Attempt}); retrying", attempt);
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapHealthChecks("/healthz");

app.Run();

public partial class Program;
