using Order.Domain.Interfaces;
using Order.Infrastructure.Data;
using Order.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IOrderRepository, OrderRepository>();

builder.Services.AddHttpClient("ProductService", client =>
{
    var baseUrl = builder.Configuration["ServiceUrls:ProductService"] ?? "http://product-service:5004";
    client.BaseAddress = new Uri(baseUrl);
});

builder.Services.AddHttpClient("NotificationService", client =>
{
    var baseUrl = builder.Configuration["ServiceUrls:NotificationService"] ?? "http://notification-service:5005";
    client.BaseAddress = new Uri(baseUrl);
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapHealthChecks("/healthz");

app.Run();
