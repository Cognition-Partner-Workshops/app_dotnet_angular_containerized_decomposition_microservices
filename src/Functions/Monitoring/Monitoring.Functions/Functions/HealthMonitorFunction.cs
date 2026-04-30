using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monitoring.Functions.Configuration;
using Monitoring.Functions.Models;
using Monitoring.Functions.Services;

namespace Monitoring.Functions.Functions;

public sealed class HealthMonitorFunction
{
    private readonly IAppInsightsQueryService _queryService;
    private readonly IAiAnalysisService _aiService;
    private readonly IAlertService _alertService;
    private readonly MonitoringOptions _options;
    private readonly ILogger<HealthMonitorFunction> _logger;

    public HealthMonitorFunction(
        IAppInsightsQueryService queryService,
        IAiAnalysisService aiService,
        IAlertService alertService,
        IOptions<MonitoringOptions> options,
        ILogger<HealthMonitorFunction> logger)
    {
        _queryService = queryService;
        _aiService = aiService;
        _alertService = alertService;
        _options = options.Value;
        _logger = logger;
    }

    [Function("HealthMonitor")]
    public async Task RunAsync(
        [TimerTrigger("0 */30 * * * *")] TimerInfo timer,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Health monitoring cycle started at {Time}", DateTime.UtcNow);

        try
        {
            var monitoringPeriod = TimeSpan.FromHours(1);
            var allTelemetry = await _queryService.GetAllServicesTelemetryAsync(
                monitoringPeriod, cancellationToken);

            var serviceStatuses = new List<ServiceHealthStatus>();
            var activeAlerts = new List<MonitoringAlert>();

            foreach (var telemetry in allTelemetry)
            {
                var state = DetermineHealthState(telemetry);
                var insight = await _aiService.GenerateHealthInsightAsync(telemetry, cancellationToken);
                var anomalies = await _aiService.DetectAnomaliesAsync(telemetry, cancellationToken);

                serviceStatuses.Add(new ServiceHealthStatus(
                    ServiceName: telemetry.ServiceName,
                    State: state,
                    UptimePercent: CalculateUptime(telemetry),
                    AverageResponseTimeMs: telemetry.AverageResponseTimeMs,
                    FailureRatePercent: telemetry.FailureRatePercent,
                    ActiveAnomalies: anomalies.Count,
                    LastChecked: DateTime.UtcNow,
                    AiInsight: insight
                ));

                if (anomalies.Count > 0)
                {
                    var alert = await _alertService.CreateAlertAsync(
                        telemetry.ServiceName,
                        state == HealthState.Unhealthy ? AlertLevel.Error : AlertLevel.Warning,
                        $"{telemetry.ServiceName} health: {state}",
                        insight,
                        anomalies,
                        cancellationToken);
                    activeAlerts.Add(alert);
                }
            }

            var overallStatus = DetermineOverallStatus(serviceStatuses);
            var aiSummary = await _aiService.GeneratePlatformSummaryAsync(allTelemetry, cancellationToken);

            var report = new HealthReport(
                GeneratedAt: DateTime.UtcNow,
                OverallStatus: overallStatus,
                AiSummary: aiSummary,
                Services: serviceStatuses,
                ActiveAlerts: activeAlerts,
                Platform: new PlatformMetrics(
                    TotalServicesMonitored: allTelemetry.Count,
                    TotalRequestsLast24h: allTelemetry.Sum(t => t.TotalRequests),
                    OverallFailureRatePercent: allTelemetry.Count > 0
                        ? allTelemetry.Average(t => t.FailureRatePercent) : 0,
                    AverageP95ResponseTimeMs: allTelemetry.Count > 0
                        ? allTelemetry.Average(t => t.P95ResponseTimeMs) : 0,
                    ActiveAnomalies: serviceStatuses.Sum(s => s.ActiveAnomalies),
                    AlertsTriggeredLast24h: activeAlerts.Count
                )
            );

            await _alertService.SendHealthReportNotificationAsync(report, cancellationToken);

            _logger.LogInformation(
                "Health report generated. Overall: {Status}, Services: {Count}, Alerts: {Alerts}",
                overallStatus, serviceStatuses.Count, activeAlerts.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health monitoring cycle failed");
            throw;
        }
    }

    private HealthState DetermineHealthState(ServiceTelemetrySummary telemetry)
    {
        if (telemetry.FailureRatePercent > _options.FailureRateThresholdPercent * 4)
            return HealthState.Unhealthy;
        if (telemetry.FailureRatePercent > _options.FailureRateThresholdPercent)
            return HealthState.Degraded;
        if (telemetry.P95ResponseTimeMs > _options.ResponseTimeThresholdMs * 2)
            return HealthState.Degraded;
        if (telemetry.TotalRequests == 0)
            return HealthState.Unknown;
        return HealthState.Healthy;
    }

    private static OverallHealthStatus DetermineOverallStatus(List<ServiceHealthStatus> services)
    {
        if (services.Any(s => s.State == HealthState.Unhealthy))
            return OverallHealthStatus.Unhealthy;
        if (services.Any(s => s.State == HealthState.Degraded))
            return OverallHealthStatus.Degraded;
        return OverallHealthStatus.Healthy;
    }

    private static double CalculateUptime(ServiceTelemetrySummary telemetry)
    {
        if (telemetry.TotalRequests == 0)
            return 100.0;
        return 100.0 - telemetry.FailureRatePercent;
    }
}
