namespace Monitoring.Functions.Models;

public record HealthReport(
    DateTime GeneratedAt,
    OverallHealthStatus OverallStatus,
    string AiSummary,
    List<ServiceHealthStatus> Services,
    List<MonitoringAlert> ActiveAlerts,
    PlatformMetrics Platform
);

public record ServiceHealthStatus(
    string ServiceName,
    HealthState State,
    double UptimePercent,
    double AverageResponseTimeMs,
    double FailureRatePercent,
    int ActiveAnomalies,
    DateTime LastChecked,
    string AiInsight
);

public record PlatformMetrics(
    int TotalServicesMonitored,
    long TotalRequestsLast24h,
    double OverallFailureRatePercent,
    double AverageP95ResponseTimeMs,
    int ActiveAnomalies,
    int AlertsTriggeredLast24h
);

public enum OverallHealthStatus
{
    Healthy,
    Degraded,
    Unhealthy
}

public enum HealthState
{
    Healthy,
    Degraded,
    Unhealthy,
    Unknown
}
