namespace Monitoring.Agent.Models;

public record AnomalyReport(
    string ServiceName,
    DateTime DetectedAt,
    AnomalySeverity Severity,
    string Category,
    string Description,
    double CurrentValue,
    double ThresholdValue,
    string RecommendedAction);

public enum AnomalySeverity
{
    Info,
    Warning,
    Critical
}

public record ServiceHealthReport(
    string ServiceName,
    DateTime Timestamp,
    HealthScore HealthScore,
    ServiceStatus Status,
    PerformanceMetrics Performance,
    ResourceUtilization Resources,
    List<AnomalyReport> ActiveAnomalies,
    List<string> Recommendations);

public record HealthScore(
    double Overall,
    double Availability,
    double Performance,
    double ErrorRate,
    double ResourceUsage);

public enum ServiceStatus
{
    Healthy,
    Degraded,
    Unhealthy,
    Unknown
}

public record PerformanceMetrics(
    double AverageResponseTimeMs,
    double P95ResponseTimeMs,
    int RequestsPerMinute,
    double ErrorRatePercent,
    int ActiveConnections);

public record ResourceUtilization(
    double CpuPercent,
    double MemoryMb,
    double MemoryPercent,
    long GcGen0Collections,
    long GcGen1Collections,
    long GcGen2Collections,
    double GcTotalMemoryMb);

public record AiInsight(
    string InsightId,
    DateTime GeneratedAt,
    InsightCategory Category,
    string Title,
    string Description,
    InsightPriority Priority,
    List<string> AffectedServices,
    List<string> ActionItems);

public enum InsightCategory
{
    Performance,
    Reliability,
    Scalability,
    CostOptimization,
    Security
}

public enum InsightPriority
{
    Low,
    Medium,
    High,
    Urgent
}

public record MonitoringDashboard(
    DateTime GeneratedAt,
    string OverallStatus,
    double SystemHealthScore,
    List<ServiceHealthReport> Services,
    List<AnomalyReport> RecentAnomalies,
    List<AiInsight> Insights,
    Dictionary<string, object> SystemMetrics);
