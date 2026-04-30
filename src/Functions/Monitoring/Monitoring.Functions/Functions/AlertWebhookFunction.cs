using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Monitoring.Functions.Models;
using Monitoring.Functions.Services;

namespace Monitoring.Functions.Functions;

public sealed class AlertWebhookFunction
{
    private readonly IAppInsightsQueryService _queryService;
    private readonly IAiAnalysisService _aiService;
    private readonly IAlertService _alertService;
    private readonly ILogger<AlertWebhookFunction> _logger;

    public AlertWebhookFunction(
        IAppInsightsQueryService queryService,
        IAiAnalysisService aiService,
        IAlertService alertService,
        ILogger<AlertWebhookFunction> logger)
    {
        _queryService = queryService;
        _aiService = aiService;
        _alertService = alertService;
        _logger = logger;
    }

    [Function("AlertWebhook")]
    public async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "monitoring/alerts/webhook")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Alert webhook triggered");

        try
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync(cancellationToken);
            var alertPayload = JsonSerializer.Deserialize<AppInsightsAlertPayload>(body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (alertPayload is null)
                return new BadRequestObjectResult(new { error = "Invalid alert payload" });

            var serviceName = alertPayload.Data?.Context?.ResourceName ?? "unknown-service";

            var telemetry = await _queryService.GetServiceTelemetryAsync(
                serviceName, TimeSpan.FromMinutes(30), cancellationToken);

            var anomalies = await _aiService.DetectAnomaliesAsync(telemetry, cancellationToken);
            var exceptionAnalysis = await _aiService.AnalyzeExceptionPatternAsync(
                telemetry.TopExceptions, serviceName, cancellationToken);

            var alert = await _alertService.CreateAlertAsync(
                serviceName,
                MapSeverity(alertPayload.Data?.Context?.Severity),
                alertPayload.Data?.Context?.Name ?? "App Insights Alert",
                $"Alert triggered: {alertPayload.Data?.Context?.Description}. " +
                $"AI Analysis: {exceptionAnalysis}",
                anomalies,
                cancellationToken);

            await _alertService.SendAlertNotificationAsync(alert, cancellationToken);

            return new OkObjectResult(new
            {
                alertId = alert.AlertId,
                processed = true,
                anomaliesDetected = anomalies.Count,
                aiEnriched = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process alert webhook");
            return new StatusCodeResult(500);
        }
    }

    [Function("TriggerManualAnalysis")]
    public async Task<IActionResult> TriggerManualAnalysisAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "monitoring/analyze/{serviceName}")] HttpRequest req,
        string serviceName,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Manual analysis triggered for {Service}", serviceName);

        var telemetry = await _queryService.GetServiceTelemetryAsync(
            serviceName, TimeSpan.FromHours(1), cancellationToken);

        var anomalies = await _aiService.DetectAnomaliesAsync(telemetry, cancellationToken);
        var insight = await _aiService.GenerateHealthInsightAsync(telemetry, cancellationToken);
        var exceptionAnalysis = await _aiService.AnalyzeExceptionPatternAsync(
            telemetry.TopExceptions, serviceName, cancellationToken);

        if (anomalies.Count > 0)
        {
            var maxSeverity = anomalies.Max(a => a.Severity);
            var alert = await _alertService.CreateAlertAsync(
                serviceName,
                maxSeverity == AnomalySeverity.Critical ? AlertLevel.Critical : AlertLevel.Warning,
                $"Manual analysis: {serviceName}",
                insight,
                anomalies,
                cancellationToken);

            await _alertService.SendAlertNotificationAsync(alert, cancellationToken);
        }

        return new OkObjectResult(new
        {
            serviceName,
            telemetry,
            healthInsight = insight,
            exceptionAnalysis,
            anomalies,
            analyzedAt = DateTime.UtcNow
        });
    }

    private static AlertLevel MapSeverity(string? severity) => severity?.ToLowerInvariant() switch
    {
        "sev0" or "critical" => AlertLevel.Critical,
        "sev1" or "error" => AlertLevel.Error,
        "sev2" or "warning" => AlertLevel.Warning,
        _ => AlertLevel.Information
    };
}

public record AppInsightsAlertPayload(
    string? SchemaId,
    AppInsightsAlertData? Data
);

public record AppInsightsAlertData(
    AppInsightsAlertContext? Context
);

public record AppInsightsAlertContext(
    string? Name,
    string? Description,
    string? ResourceName,
    string? ResourceGroupName,
    string? Severity,
    string? ConditionType
);
