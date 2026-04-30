using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Monitoring.Functions.Models;
using Monitoring.Functions.Services;

namespace Monitoring.Functions.Functions;

public sealed class MonitoringDashboardFunction
{
    private readonly IAppInsightsQueryService _queryService;
    private readonly IAiAnalysisService _aiService;
    private readonly ILogger<MonitoringDashboardFunction> _logger;

    public MonitoringDashboardFunction(
        IAppInsightsQueryService queryService,
        IAiAnalysisService aiService,
        ILogger<MonitoringDashboardFunction> logger)
    {
        _queryService = queryService;
        _aiService = aiService;
        _logger = logger;
    }

    [Function("GetHealthReport")]
    public async Task<IActionResult> GetHealthReportAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "monitoring/health")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Health report requested");

        var periodParam = req.Query["period"].FirstOrDefault() ?? "1h";
        var period = ParsePeriod(periodParam);

        var allTelemetry = await _queryService.GetAllServicesTelemetryAsync(period, cancellationToken);

        var serviceStatuses = new List<ServiceHealthStatus>();
        foreach (var telemetry in allTelemetry)
        {
            var insight = await _aiService.GenerateHealthInsightAsync(telemetry, cancellationToken);
            var anomalies = await _aiService.DetectAnomaliesAsync(telemetry, cancellationToken);

            serviceStatuses.Add(new ServiceHealthStatus(
                ServiceName: telemetry.ServiceName,
                State: telemetry.FailureRatePercent > 20 ? HealthState.Unhealthy :
                       telemetry.FailureRatePercent > 5 ? HealthState.Degraded : HealthState.Healthy,
                UptimePercent: 100.0 - telemetry.FailureRatePercent,
                AverageResponseTimeMs: telemetry.AverageResponseTimeMs,
                FailureRatePercent: telemetry.FailureRatePercent,
                ActiveAnomalies: anomalies.Count,
                LastChecked: DateTime.UtcNow,
                AiInsight: insight
            ));
        }

        var aiSummary = await _aiService.GeneratePlatformSummaryAsync(allTelemetry, cancellationToken);

        var report = new HealthReport(
            GeneratedAt: DateTime.UtcNow,
            OverallStatus: serviceStatuses.Any(s => s.State == HealthState.Unhealthy)
                ? OverallHealthStatus.Unhealthy
                : serviceStatuses.Any(s => s.State == HealthState.Degraded)
                    ? OverallHealthStatus.Degraded
                    : OverallHealthStatus.Healthy,
            AiSummary: aiSummary,
            Services: serviceStatuses,
            ActiveAlerts: [],
            Platform: new PlatformMetrics(
                TotalServicesMonitored: allTelemetry.Count,
                TotalRequestsLast24h: allTelemetry.Sum(t => t.TotalRequests),
                OverallFailureRatePercent: allTelemetry.Count > 0
                    ? allTelemetry.Average(t => t.FailureRatePercent) : 0,
                AverageP95ResponseTimeMs: allTelemetry.Count > 0
                    ? allTelemetry.Average(t => t.P95ResponseTimeMs) : 0,
                ActiveAnomalies: serviceStatuses.Sum(s => s.ActiveAnomalies),
                AlertsTriggeredLast24h: 0
            )
        );

        return new OkObjectResult(report);
    }

    [Function("GetServiceTelemetry")]
    public async Task<IActionResult> GetServiceTelemetryAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "monitoring/services/{serviceName}")] HttpRequest req,
        string serviceName,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Telemetry requested for {Service}", serviceName);

        var periodParam = req.Query["period"].FirstOrDefault() ?? "1h";
        var period = ParsePeriod(periodParam);

        var telemetry = await _queryService.GetServiceTelemetryAsync(serviceName, period, cancellationToken);
        var insight = await _aiService.GenerateHealthInsightAsync(telemetry, cancellationToken);
        var anomalies = await _aiService.DetectAnomaliesAsync(telemetry, cancellationToken);
        var exceptionAnalysis = await _aiService.AnalyzeExceptionPatternAsync(
            telemetry.TopExceptions, serviceName, cancellationToken);

        return new OkObjectResult(new
        {
            telemetry,
            aiInsight = insight,
            anomalies,
            exceptionAnalysis
        });
    }

    [Function("GetAnomalies")]
    public async Task<IActionResult> GetAnomaliesAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "monitoring/anomalies")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Anomaly report requested");

        var periodParam = req.Query["period"].FirstOrDefault() ?? "30m";
        var period = ParsePeriod(periodParam);

        var allTelemetry = await _queryService.GetAllServicesTelemetryAsync(period, cancellationToken);
        var allAnomalies = new List<AnomalyResult>();

        foreach (var telemetry in allTelemetry)
        {
            var anomalies = await _aiService.DetectAnomaliesAsync(telemetry, cancellationToken);
            allAnomalies.AddRange(anomalies);
        }

        return new OkObjectResult(new
        {
            period = periodParam,
            totalAnomalies = allAnomalies.Count,
            bySeverity = new
            {
                critical = allAnomalies.Count(a => a.Severity == AnomalySeverity.Critical),
                warning = allAnomalies.Count(a => a.Severity == AnomalySeverity.Warning),
                info = allAnomalies.Count(a => a.Severity == AnomalySeverity.Info)
            },
            anomalies = allAnomalies.OrderByDescending(a => a.Severity).ThenByDescending(a => a.DetectedAt)
        });
    }

    private static TimeSpan ParsePeriod(string period) => period.ToLowerInvariant() switch
    {
        "5m" => TimeSpan.FromMinutes(5),
        "15m" => TimeSpan.FromMinutes(15),
        "30m" => TimeSpan.FromMinutes(30),
        "1h" => TimeSpan.FromHours(1),
        "6h" => TimeSpan.FromHours(6),
        "12h" => TimeSpan.FromHours(12),
        "24h" or "1d" => TimeSpan.FromHours(24),
        "7d" => TimeSpan.FromDays(7),
        _ => TimeSpan.FromHours(1)
    };
}
