namespace AIMonitoringAgent.Configuration;

public class MonitoringConfiguration
{
    public const string SectionName = "Monitoring";

    public string ApplicationInsightsConnectionString { get; set; } = string.Empty;
    public int HealthCheckIntervalSeconds { get; set; } = 30;
    public double AnomalySensitivityMultiplier { get; set; } = 2.0;

    public Dictionary<string, string> ServiceEndpoints { get; set; } = new()
    {
        ["identity-service"] = "http://localhost:5001/healthz",
        ["customer-service"] = "http://localhost:5002/healthz",
        ["order-service"] = "http://localhost:5003/healthz",
        ["product-service"] = "http://localhost:5004/healthz",
        ["notification-service"] = "http://localhost:5005/healthz",
        ["api-gateway"] = "http://localhost:5000/healthz"
    };

    public List<DefaultAlertRule> DefaultAlertRules { get; set; } = new();
}

public class DefaultAlertRule
{
    public string Name { get; set; } = string.Empty;
    public string MetricName { get; set; } = string.Empty;
    public string? ServiceName { get; set; }
    public string Condition { get; set; } = "GreaterThan";
    public double Threshold { get; set; }
    public string Severity { get; set; } = "Warning";
}
