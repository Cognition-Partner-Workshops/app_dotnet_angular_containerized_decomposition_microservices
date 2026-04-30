using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using AIMonitoringAgent.Models;

namespace AIMonitoringAgent.Services;

public interface IAppInsightsTelemetryService
{
    void TrackAIModelInvocation(AIModelMetrics metrics);
    void TrackServiceHealth(ServiceHealthStatus status);
    void TrackAnomaly(AnomalyDetectionResult anomaly);
    void TrackAlert(AlertNotification alert);
    void TrackCustomMetric(string name, double value, Dictionary<string, string>? properties = null);
    void TrackDependency(string dependencyType, string target, string name, double durationMs, bool success);
    void TrackException(Exception exception, Dictionary<string, string>? properties = null);
    void TrackEvent(string eventName, Dictionary<string, string>? properties = null, Dictionary<string, double>? metrics = null);
    void Flush();
}

public class AppInsightsTelemetryService : IAppInsightsTelemetryService
{
    private readonly TelemetryClient _telemetryClient;
    private readonly ILogger<AppInsightsTelemetryService> _logger;

    public AppInsightsTelemetryService(
        TelemetryClient telemetryClient,
        ILogger<AppInsightsTelemetryService> logger)
    {
        _telemetryClient = telemetryClient;
        _logger = logger;
    }

    public void TrackAIModelInvocation(AIModelMetrics metrics)
    {
        var properties = new Dictionary<string, string>
        {
            ["ModelName"] = metrics.ModelName,
            ["ModelVersion"] = metrics.ModelVersion,
            ["IsSuccessful"] = metrics.IsSuccessful.ToString()
        };

        if (!string.IsNullOrEmpty(metrics.ErrorMessage))
            properties["ErrorMessage"] = metrics.ErrorMessage;

        foreach (var kvp in metrics.CustomProperties)
            properties[kvp.Key] = kvp.Value;

        var telemetryMetrics = new Dictionary<string, double>
        {
            ["LatencyMs"] = metrics.Latency,
            ["TokensUsed"] = metrics.TokensUsed,
            ["PromptTokens"] = metrics.PromptTokens,
            ["CompletionTokens"] = metrics.CompletionTokens
        };

        _telemetryClient.TrackEvent("AIModelInvocation", properties, telemetryMetrics);

        _telemetryClient.GetMetric("AIModel.Latency", "ModelName")
            .TrackValue(metrics.Latency, metrics.ModelName);
        _telemetryClient.GetMetric("AIModel.TokensUsed", "ModelName")
            .TrackValue(metrics.TokensUsed, metrics.ModelName);

        if (!metrics.IsSuccessful)
        {
            _telemetryClient.GetMetric("AIModel.Failures", "ModelName")
                .TrackValue(1, metrics.ModelName);
        }

        _logger.LogDebug("Tracked AI model invocation: {ModelName} v{Version}, Latency={Latency}ms",
            metrics.ModelName, metrics.ModelVersion, metrics.Latency);
    }

    public void TrackServiceHealth(ServiceHealthStatus status)
    {
        var properties = new Dictionary<string, string>
        {
            ["ServiceName"] = status.ServiceName,
            ["Endpoint"] = status.Endpoint,
            ["State"] = status.State.ToString(),
            ["HttpStatusCode"] = status.HttpStatusCode.ToString()
        };

        if (!string.IsNullOrEmpty(status.ErrorDetails))
            properties["ErrorDetails"] = status.ErrorDetails;

        var metrics = new Dictionary<string, double>
        {
            ["ResponseTimeMs"] = status.ResponseTimeMs,
            ["ConsecutiveFailures"] = status.ConsecutiveFailures
        };

        _telemetryClient.TrackEvent("ServiceHealthCheck", properties, metrics);

        _telemetryClient.GetMetric("Service.ResponseTime", "ServiceName")
            .TrackValue(status.ResponseTimeMs, status.ServiceName);

        var availability = new AvailabilityTelemetry
        {
            Name = $"{status.ServiceName} Health Check",
            Duration = TimeSpan.FromMilliseconds(status.ResponseTimeMs),
            Success = status.State == HealthState.Healthy,
            RunLocation = "AIMonitoringAgent",
            Message = status.State.ToString(),
            Timestamp = status.LastCheckedAt
        };

        _telemetryClient.TrackAvailability(availability);
    }

    public void TrackAnomaly(AnomalyDetectionResult anomaly)
    {
        var properties = new Dictionary<string, string>
        {
            ["MetricName"] = anomaly.MetricName,
            ["ServiceName"] = anomaly.ServiceName,
            ["Severity"] = anomaly.Severity.ToString(),
            ["IsAnomaly"] = anomaly.IsAnomaly.ToString(),
            ["Description"] = anomaly.Description
        };

        var metrics = new Dictionary<string, double>
        {
            ["CurrentValue"] = anomaly.CurrentValue,
            ["ExpectedValue"] = anomaly.ExpectedValue,
            ["DeviationPercentage"] = anomaly.DeviationPercentage
        };

        _telemetryClient.TrackEvent("AnomalyDetected", properties, metrics);

        if (anomaly.Severity >= AnomalySeverity.High)
        {
            _logger.LogWarning("High severity anomaly detected: {Description}", anomaly.Description);
        }
    }

    public void TrackAlert(AlertNotification alert)
    {
        var properties = new Dictionary<string, string>
        {
            ["AlertRuleId"] = alert.AlertRuleId,
            ["AlertRuleName"] = alert.AlertRuleName,
            ["MetricName"] = alert.MetricName,
            ["Severity"] = alert.Severity.ToString(),
            ["Message"] = alert.Message
        };

        if (!string.IsNullOrEmpty(alert.ServiceName))
            properties["ServiceName"] = alert.ServiceName;

        var metrics = new Dictionary<string, double>
        {
            ["CurrentValue"] = alert.CurrentValue,
            ["Threshold"] = alert.Threshold
        };

        _telemetryClient.TrackEvent("AlertFired", properties, metrics);

        _logger.LogWarning("Alert fired: {AlertName} - {Message}", alert.AlertRuleName, alert.Message);
    }

    public void TrackCustomMetric(string name, double value, Dictionary<string, string>? properties = null)
    {
        var metricTelemetry = new MetricTelemetry(name, value);

        if (properties != null)
        {
            foreach (var kvp in properties)
                metricTelemetry.Properties[kvp.Key] = kvp.Value;
        }

        _telemetryClient.TrackMetric(metricTelemetry);
    }

    public void TrackDependency(string dependencyType, string target, string name, double durationMs, bool success)
    {
        _telemetryClient.TrackDependency(dependencyType, target, name,
            string.Empty, DateTimeOffset.UtcNow,
            TimeSpan.FromMilliseconds(durationMs), "200", success);
    }

    public void TrackException(Exception exception, Dictionary<string, string>? properties = null)
    {
        _telemetryClient.TrackException(exception, properties);
        _logger.LogError(exception, "Exception tracked in Application Insights");
    }

    public void TrackEvent(string eventName, Dictionary<string, string>? properties = null, Dictionary<string, double>? metrics = null)
    {
        _telemetryClient.TrackEvent(eventName, properties, metrics);
    }

    public void Flush()
    {
        _telemetryClient.Flush();
    }
}
