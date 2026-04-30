namespace Shared.Monitoring;

public class AppInsightsConfig
{
    public const string SectionName = "ApplicationInsights";

    public string ConnectionString { get; set; } = string.Empty;
    public string CloudRoleName { get; set; } = string.Empty;
    public bool EnableAdaptiveSampling { get; set; } = true;
    public double SamplingPercentage { get; set; } = 100;
    public bool EnableDependencyTracking { get; set; } = true;
    public bool EnablePerformanceCounters { get; set; } = true;
    public bool EnableAiDiagnostics { get; set; } = true;
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
