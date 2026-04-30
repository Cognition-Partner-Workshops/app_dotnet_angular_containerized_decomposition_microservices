using Shared.Monitoring.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddHealthChecks();

builder.Services.AddAppInsightsMonitoring(builder.Configuration, "ApiGateway");

var app = builder.Build();

app.UseAppInsightsMonitoring();
app.MapReverseProxy();
app.MapHealthChecks("/healthz");

app.Run();
