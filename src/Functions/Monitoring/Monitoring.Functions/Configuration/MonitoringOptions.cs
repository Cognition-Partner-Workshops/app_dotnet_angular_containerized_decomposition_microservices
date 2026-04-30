namespace Monitoring.Functions.Configuration;

public sealed class MonitoringOptions
{
    public string ApplicationInsightsConnectionString { get; set; } = string.Empty;
    public string WorkspaceId { get; set; } = string.Empty;
    public string AzureOpenAIEndpoint { get; set; } = string.Empty;
    public string AzureOpenAIDeployment { get; set; } = "gpt-4o";
    public string AlertWebhookUrl { get; set; } = string.Empty;
    public int AnomalyDetectionIntervalMinutes { get; set; } = 5;
    public int HealthCheckIntervalMinutes { get; set; } = 2;
    public double FailureRateThresholdPercent { get; set; } = 5.0;
    public double ResponseTimeThresholdMs { get; set; } = 2000;
    public string MonitoredServices { get; set; } = string.Empty;

    public IReadOnlyList<string> GetMonitoredServiceNames() =>
        MonitoredServices
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

    public IReadOnlyList<MonitoredServiceConfig> GetServiceConfigs() =>
        GetMonitoredServiceNames()
            .Select(name => new MonitoredServiceConfig(name))
            .ToList();
}

public record MonitoredServiceConfig(string ServiceName)
{
    public string CloudRoleName => ServiceName;
}
