var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseMiddleware<Shared.Infrastructure.Middleware.CorrelationIdMiddleware>();

app.MapReverseProxy();
app.MapHealthChecks("/healthz");

app.Run();
