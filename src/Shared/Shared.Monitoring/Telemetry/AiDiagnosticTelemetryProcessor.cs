using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Shared.Monitoring.Telemetry;

/// <summary>
/// Enriches request and dependency telemetry with AI diagnostic metadata.
/// Flags slow requests and failed dependencies for anomaly detection.
/// </summary>
public class AiDiagnosticTelemetryProcessor : ITelemetryProcessor
{
    private readonly ITelemetryProcessor _next;
    private readonly double _slowRequestThresholdMs;

    public AiDiagnosticTelemetryProcessor(
        ITelemetryProcessor next,
        double slowRequestThresholdMs = 2000)
    {
        _next = next;
        _slowRequestThresholdMs = slowRequestThresholdMs;
    }

    public void Process(ITelemetry item)
    {
        if (item is RequestTelemetry request)
        {
            var durationMs = request.Duration.TotalMilliseconds;
            if (request is ISupportProperties props)
            {
                props.Properties["AI.IsSlowRequest"] =
                    (durationMs > _slowRequestThresholdMs).ToString();
                props.Properties["AI.DurationBucket"] = GetDurationBucket(durationMs);
            }
        }

        if (item is DependencyTelemetry dependency)
        {
            if (!dependency.Success.GetValueOrDefault(true)
                && dependency is ISupportProperties depProps)
            {
                depProps.Properties["AI.FailedDependency"] = "true";
                depProps.Properties["AI.DependencyTarget"] = dependency.Target;
            }
        }

        if (item is ExceptionTelemetry exception)
        {
            if (exception is ISupportProperties exProps)
            {
                exProps.Properties["AI.ExceptionType"] =
                    exception.Exception?.GetType().Name ?? "Unknown";
            }
        }

        _next.Process(item);
    }

    private static string GetDurationBucket(double ms) => ms switch
    {
        < 100 => "Fast",
        < 500 => "Normal",
        < 2000 => "Slow",
        _ => "Critical"
    };
}
