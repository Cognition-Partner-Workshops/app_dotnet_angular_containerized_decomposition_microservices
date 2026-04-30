using AppInsightsMonitoringAgent.AiEngine;
using AppInsightsMonitoringAgent.HealthChecks;
using AppInsightsMonitoringAgent.Metrics;
using AppInsightsMonitoringAgent.Middleware;
using AppInsightsMonitoringAgent.Models;
using AppInsightsMonitoringAgent.Telemetry;
using Microsoft.ApplicationInsights.Extensibility;

var builder = WebApplication.CreateBuilder(args);

// ── Core services ──
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();

// ── Application Insights telemetry ──
var aiConfig = new AppInsightsConfig();
builder.Configuration.GetSection(AppInsightsConfig.SectionName).Bind(aiConfig);

if (!string.IsNullOrEmpty(aiConfig.ConnectionString))
{
    builder.Services.AddApplicationInsightsTelemetry(options =>
    {
        options.ConnectionString = aiConfig.ConnectionString;
        options.EnableAdaptiveSampling = aiConfig.EnableAdaptiveSampling;
    });
}
else
{
    builder.Services.AddApplicationInsightsTelemetry();
}

builder.Services.AddSingleton<ITelemetryInitializer>(
    new ServiceTelemetryInitializer(aiConfig.CloudRoleName));
builder.Services.AddSingleton<ITelemetryInitializer, CorrelationTelemetryInitializer>();
builder.Services.AddApplicationInsightsTelemetryProcessor<AiDiagnosticTelemetryProcessor>();

// ── Metrics collector ──
builder.Services.AddSingleton(sp =>
{
    var client = sp.GetRequiredService<Microsoft.ApplicationInsights.TelemetryClient>();
    return new ServiceMetricsCollector(client, aiConfig.CloudRoleName);
});

// ── Health checks ──
builder.Services.AddHealthChecks()
    .AddCheck<AppInsightsHealthCheck>("app-insights");

// ── HTTP client for probing services ──
builder.Services.AddHttpClient("MonitoringAgent", client =>
{
    client.Timeout = TimeSpan.FromSeconds(5);
});

// ── AI engines ──
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

app.UseMiddleware<AiTelemetryMiddleware>();
app.MapControllers();
app.MapHealthChecks("/healthz");

app.Run();
