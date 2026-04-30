using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace AppInsightsMonitoringAgent.Telemetry;

/// <summary>
/// Propagates X-Correlation-ID from incoming HTTP headers into Application
/// Insights telemetry as a custom property for distributed trace linking.
/// </summary>
public class CorrelationTelemetryInitializer : ITelemetryInitializer
{
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationTelemetryInitializer(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Initialize(ITelemetry telemetry)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
            return;

        if (httpContext.Request.Headers.TryGetValue(CorrelationIdHeader, out var correlationId)
            && telemetry is ISupportProperties propTelemetry)
        {
            propTelemetry.Properties["CorrelationId"] = correlationId.ToString();
        }
    }
}
