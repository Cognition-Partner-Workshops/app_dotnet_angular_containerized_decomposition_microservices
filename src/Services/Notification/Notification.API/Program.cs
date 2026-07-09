using Notification.API.Services;
using Notification.Domain.Interfaces;
using Notification.Infrastructure.Data;
using Notification.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<NotificationDbContext>("database", tags: new[] { "ready" });

builder.Services.AddDbContext<NotificationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<NotificationRenderer>();
builder.Services.AddScoped<OrderEventConsumer>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    try
    {
        db.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        // Liveness must not depend on the database being reachable at startup;
        // readiness (/ready) reflects DB availability instead.
        app.Logger.LogWarning(ex, "Database initialization skipped; database unavailable at startup.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
// Liveness: process is up; no dependency checks.
app.MapHealthChecks("/health",  new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/healthz", new HealthCheckOptions { Predicate = _ => false });
// Readiness: dependencies (DB) reachable -> 200, else 503.
app.MapHealthChecks("/ready",   new HealthCheckOptions { Predicate = c => c.Tags.Contains("ready") });

app.Run();
