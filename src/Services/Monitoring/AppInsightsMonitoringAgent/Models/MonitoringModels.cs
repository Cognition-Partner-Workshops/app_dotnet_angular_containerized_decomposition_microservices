namespace AppInsightsMonitoringAgent.Models;

// ── Configuration ──

public class AppInsightsConfig
{
    public const string SectionName = "ApplicationInsights";

    public string ConnectionString { get; set; } = string.Empty;
    public string CloudRoleName { get; set; } = "MonitoringAgent";
    public bool EnableAdaptiveSampling { get; set; } = true;
    public bool EnableDependencyTracking { get; set; } = true;
    public bool EnablePerformanceCounters { get; set; } = true;
    public int MetricCollectionIntervalSeconds { get; set; } = 60;
    public AnomalyDetectionConfig AnomalyDetection { get; set; } = new();
}

public class AnomalyDetectionConfig
{
    public double ResponseTimeThresholdMs { get; set; } = 2000;
    public double ErrorRateThresholdPercent { get; set; } = 5;
    public double CpuThresholdPercent { get; set; } = 80;
    public double MemoryThresholdPercent { get; set; } = 85;
    public int EvaluationWindowMinutes { get; set; } = 5;
}

// ── Health Snapshot (internal metric collection) ──

public record ServiceHealthSnapshot(
    string ServiceName,
    DateTime Timestamp,
    long TotalRequests,
    long FailedRequests,
    int RecentRequestCount,
    int RecentErrorCount,
    double ErrorRatePercent,
    double AverageResponseTimeMs,
    double P95ResponseTimeMs,
    double MemoryUsageMb,
    double CpuTimeSeconds,
    long TotalDependencyCalls,
    long FailedDependencyCalls);

public record RequestMetricEntry(
    DateTime Timestamp,
    string Endpoint,
    double DurationMs,
    bool Success,
    int StatusCode);

// ── Anomaly Detection ──

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

// ── Health Scoring ──

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

// ── AI Insights ──

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

// ── Reports & Dashboard ──

public record ServiceHealthReport(
    string ServiceName,
    DateTime Timestamp,
    HealthScore HealthScore,
    ServiceStatus Status,
    PerformanceMetrics Performance,
    ResourceUtilization Resources,
    List<AnomalyReport> ActiveAnomalies,
    List<string> Recommendations);

public record MonitoringDashboard(
    DateTime GeneratedAt,
    string OverallStatus,
    double SystemHealthScore,
    List<ServiceHealthReport> Services,
    List<AnomalyReport> RecentAnomalies,
    List<AiInsight> Insights,
    Dictionary<string, object> SystemMetrics);
