namespace Monitoring.Functions.Models;

public record AnomalyResult(
    string ServiceName,
    AnomalyType Type,
    AnomalySeverity Severity,
    string Description,
    string AiAnalysis,
    string RecommendedAction,
    double CurrentValue,
    double BaselineValue,
    double DeviationPercent,
    DateTime DetectedAt
);

public enum AnomalyType
{
    HighFailureRate,
    ResponseTimeSpike,
    ExceptionBurst,
    DependencyDegradation,
    TrafficAnomaly,
    MemoryLeak,
    CpuSpike
}

public enum AnomalySeverity
{
    Info,
    Warning,
    Critical
}
