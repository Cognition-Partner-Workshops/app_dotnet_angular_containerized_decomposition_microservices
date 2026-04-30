namespace AIMonitoringAgent.Models;

public class ServiceHealthStatus
{
    public string ServiceName { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public HealthState State { get; set; } = HealthState.Unknown;
    public double ResponseTimeMs { get; set; }
    public int HttpStatusCode { get; set; }
    public string? ErrorDetails { get; set; }
    public DateTime LastCheckedAt { get; set; } = DateTime.UtcNow;
    public int ConsecutiveFailures { get; set; }
}

public enum HealthState
{
    Healthy,
    Degraded,
    Unhealthy,
    Unknown
}
