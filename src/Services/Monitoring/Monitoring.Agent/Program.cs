using Monitoring.Agent.Services;
using Shared.Monitoring;
using Shared.Monitoring.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAppInsightsMonitoring(builder.Configuration, "MonitoringAgent");

builder.Services.AddHttpClient("MonitoringAgent", client =>
{
    client.Timeout = TimeSpan.FromSeconds(5);
});

var anomalyConfig = new AnomalyDetectionConfig();
builder.Configuration.GetSection("ApplicationInsights:AnomalyDetection").Bind(anomalyConfig);
builder.Services.AddSingleton(anomalyConfig);

builder.Services.AddSingleton<AnomalyDetectionEngine>();
builder.Services.AddSingleton<HealthScoringEngine>();
builder.Services.AddSingleton<AiInsightsEngine>();
builder.Services.AddSingleton<ServiceHealthAggregator>();
builder.Services.AddHostedService<MonitoringBackgroundService>();

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
