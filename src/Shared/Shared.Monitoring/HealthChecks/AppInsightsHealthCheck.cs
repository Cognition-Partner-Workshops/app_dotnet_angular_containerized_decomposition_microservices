using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Shared.Monitoring.HealthChecks;

/// <summary>
/// Verifies Application Insights connectivity by flushing telemetry.
/// Reports degraded status if the connection string is not configured.
/// </summary>
public class AppInsightsHealthCheck : IHealthCheck
{
    private readonly TelemetryClient _telemetryClient;

    public AppInsightsHealthCheck(TelemetryClient telemetryClient)
    {
        _telemetryClient = telemetryClient;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(_telemetryClient.InstrumentationKey)
                && string.IsNullOrEmpty(_telemetryClient.TelemetryConfiguration.ConnectionString))
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    "Application Insights connection string is not configured."));
            }

            _telemetryClient.Flush();
            return Task.FromResult(HealthCheckResult.Healthy(
                "Application Insights telemetry channel is active."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Application Insights health check failed.", ex));
        }
    }
}
