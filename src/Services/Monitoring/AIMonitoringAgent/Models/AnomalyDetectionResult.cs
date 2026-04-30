namespace AIMonitoringAgent.Models;

public class AnomalyDetectionResult
{
    public string MetricName { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public double CurrentValue { get; set; }
    public double ExpectedValue { get; set; }
    public double DeviationPercentage { get; set; }
    public AnomalySeverity Severity { get; set; }
    public bool IsAnomaly { get; set; }
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public string Description { get; set; } = string.Empty;
}

public enum AnomalySeverity
{
    Low,
    Medium,
    High,
    Critical
}
