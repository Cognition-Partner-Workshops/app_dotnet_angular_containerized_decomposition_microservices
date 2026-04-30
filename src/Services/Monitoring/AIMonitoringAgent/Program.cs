using AIMonitoringAgent.Configuration;
using AIMonitoringAgent.Models;
using AIMonitoringAgent.Services;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Services.Configure<MonitoringConfiguration>(
    builder.Configuration.GetSection(MonitoringConfiguration.SectionName));

// Application Insights
var appInsightsConnectionString = builder.Configuration
    .GetValue<string>("Monitoring:ApplicationInsightsConnectionString");

builder.Services.AddApplicationInsightsTelemetry(options =>
{
    if (!string.IsNullOrEmpty(appInsightsConnectionString))
        options.ConnectionString = appInsightsConnectionString;
});

// Core services
builder.Services.AddSingleton<IAppInsightsTelemetryService, AppInsightsTelemetryService>();
builder.Services.AddSingleton<IAIModelMonitoringService, AIModelMonitoringService>();
builder.Services.AddSingleton<IAnomalyDetectionService, AnomalyDetectionService>();
builder.Services.AddSingleton<IAlertingService, AlertingService>();

// Health monitor with HttpClient
builder.Services.AddHttpClient<IServiceHealthMonitor, ServiceHealthMonitor>();

// Background health check service
builder.Services.AddHostedService<HealthCheckBackgroundService>();

// ASP.NET Core
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "AI Monitoring Agent API",
        Version = "v1",
        Description = "Azure Application Insights AI Monitoring Agent for microservices observability"
    });
});
builder.Services.AddHealthChecks();

// Seed default alert rules
var monitoringConfig = builder.Configuration
    .GetSection(MonitoringConfiguration.SectionName)
    .Get<MonitoringConfiguration>();

var app = builder.Build();

// Seed default alert rules from configuration
if (monitoringConfig?.DefaultAlertRules.Count > 0)
{
    var alertingService = app.Services.GetRequiredService<IAlertingService>();
    foreach (var ruleConfig in monitoringConfig.DefaultAlertRules)
    {
        var rule = new AlertRule
        {
            Name = ruleConfig.Name,
            MetricName = ruleConfig.MetricName,
            ServiceName = ruleConfig.ServiceName,
            Condition = Enum.Parse<AlertCondition>(ruleConfig.Condition),
            Threshold = ruleConfig.Threshold,
            Severity = Enum.Parse<AlertSeverityLevel>(ruleConfig.Severity)
        };
        alertingService.AddRule(rule);
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
