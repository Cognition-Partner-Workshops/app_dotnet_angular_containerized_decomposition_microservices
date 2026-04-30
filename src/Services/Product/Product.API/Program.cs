using Product.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Monitoring.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

builder.Services.AddDbContext<ProductDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAppInsightsMonitoring(builder.Configuration, "ProductService");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAppInsightsMonitoring();
app.MapControllers();
app.MapHealthChecks("/healthz");

app.Run();
