namespace Monitoring.Functions.Models;

public record MonitoringAlert(
    Guid AlertId,
    string ServiceName,
    AlertLevel Level,
    string Title,
    string Summary,
    string DetailedAnalysis,
    List<AnomalyResult> Anomalies,
    List<string> RecommendedActions,
    DateTime CreatedAt,
    AlertStatus Status
);

public enum AlertLevel
{
    Information,
    Warning,
    Error,
    Critical
}

public enum AlertStatus
{
    Active,
    Acknowledged,
    Resolved,
    Suppressed
}
