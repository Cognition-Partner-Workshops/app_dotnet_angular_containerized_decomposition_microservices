using Monitoring.Agent.Models;
using Shared.Monitoring;
using Shared.Monitoring.Metrics;

namespace Monitoring.Agent.Services;

/// <summary>
/// AI-powered anomaly detection engine that analyzes service health snapshots
/// against configurable thresholds and historical baselines.
/// Uses statistical analysis to detect performance degradation, error spikes,
/// and resource exhaustion patterns.
/// </summary>
public class AnomalyDetectionEngine
{
    private readonly AnomalyDetectionConfig _config;
    private readonly ILogger<AnomalyDetectionEngine> _logger;
    private readonly Dictionary<string, List<ServiceHealthSnapshot>> _history = new();
    private readonly object _lock = new();

    public AnomalyDetectionEngine(
        AnomalyDetectionConfig config,
        ILogger<AnomalyDetectionEngine> logger)
    {
        _config = config;
        _logger = logger;
    }

    public List<AnomalyReport> Analyze(ServiceHealthSnapshot snapshot)
    {
        var anomalies = new List<AnomalyReport>();

        RecordSnapshot(snapshot);

        DetectHighErrorRate(snapshot, anomalies);
        DetectSlowResponseTime(snapshot, anomalies);
        DetectHighMemoryUsage(snapshot, anomalies);
        DetectDependencyFailures(snapshot, anomalies);
        DetectResponseTimeSpike(snapshot, anomalies);

        return anomalies;
    }

    private void DetectHighErrorRate(ServiceHealthSnapshot snapshot, List<AnomalyReport> anomalies)
    {
        if (snapshot.RecentRequestCount == 0)
            return;

        if (snapshot.ErrorRatePercent > _config.ErrorRateThresholdPercent)
        {
            var severity = snapshot.ErrorRatePercent > _config.ErrorRateThresholdPercent * 2
                ? AnomalySeverity.Critical
                : AnomalySeverity.Warning;

            anomalies.Add(new AnomalyReport(
                snapshot.ServiceName,
                DateTime.UtcNow,
                severity,
                "ErrorRate",
                $"Error rate of {snapshot.ErrorRatePercent:F1}% exceeds threshold of {_config.ErrorRateThresholdPercent}%.",
                snapshot.ErrorRatePercent,
                _config.ErrorRateThresholdPercent,
                "Investigate recent deployments and dependency health. Check application logs for recurring exceptions."));
        }
    }

    private void DetectSlowResponseTime(ServiceHealthSnapshot snapshot, List<AnomalyReport> anomalies)
    {
        if (snapshot.RecentRequestCount == 0)
            return;

        if (snapshot.P95ResponseTimeMs > _config.ResponseTimeThresholdMs)
        {
            var severity = snapshot.P95ResponseTimeMs > _config.ResponseTimeThresholdMs * 2
                ? AnomalySeverity.Critical
                : AnomalySeverity.Warning;

            anomalies.Add(new AnomalyReport(
                snapshot.ServiceName,
                DateTime.UtcNow,
                severity,
                "ResponseTime",
                $"P95 response time of {snapshot.P95ResponseTimeMs:F0}ms exceeds threshold of {_config.ResponseTimeThresholdMs}ms.",
                snapshot.P95ResponseTimeMs,
                _config.ResponseTimeThresholdMs,
                "Profile slow endpoints. Check database query performance and downstream service latency."));
        }
    }

    private void DetectHighMemoryUsage(ServiceHealthSnapshot snapshot, List<AnomalyReport> anomalies)
    {
        var estimatedMemoryPercent = snapshot.MemoryUsageMb / 1024.0 * 100;
        if (estimatedMemoryPercent > _config.MemoryThresholdPercent)
        {
            anomalies.Add(new AnomalyReport(
                snapshot.ServiceName,
                DateTime.UtcNow,
                AnomalySeverity.Warning,
                "MemoryUsage",
                $"Memory usage of {snapshot.MemoryUsageMb:F0}MB is elevated.",
                snapshot.MemoryUsageMb,
                _config.MemoryThresholdPercent,
                "Analyze memory allocation patterns. Check for potential memory leaks using dotnet-dump or Application Insights profiler."));
        }
    }

    private void DetectDependencyFailures(ServiceHealthSnapshot snapshot, List<AnomalyReport> anomalies)
    {
        if (snapshot.TotalDependencyCalls == 0)
            return;

        var failureRate = (double)snapshot.FailedDependencyCalls / snapshot.TotalDependencyCalls * 100;
        if (failureRate > _config.ErrorRateThresholdPercent)
        {
            anomalies.Add(new AnomalyReport(
                snapshot.ServiceName,
                DateTime.UtcNow,
                AnomalySeverity.Critical,
                "DependencyFailure",
                $"Dependency failure rate of {failureRate:F1}% indicates downstream service issues.",
                failureRate,
                _config.ErrorRateThresholdPercent,
                "Check health of dependent services (database, message broker, external APIs). Verify network connectivity and circuit breaker states."));
        }
    }

    private void DetectResponseTimeSpike(ServiceHealthSnapshot snapshot, List<AnomalyReport> anomalies)
    {
        lock (_lock)
        {
            if (!_history.TryGetValue(snapshot.ServiceName, out var history) || history.Count < 3)
                return;

            var baseline = history
                .Where(h => h.RecentRequestCount > 0)
                .Select(h => h.AverageResponseTimeMs)
                .ToArray();

            if (baseline.Length < 3)
                return;

            var mean = baseline.Average();
            var stdDev = Math.Sqrt(baseline.Average(v => Math.Pow(v - mean, 2)));

            if (stdDev > 0 && snapshot.AverageResponseTimeMs > mean + 2 * stdDev)
            {
                anomalies.Add(new AnomalyReport(
                    snapshot.ServiceName,
                    DateTime.UtcNow,
                    AnomalySeverity.Warning,
                    "ResponseTimeSpike",
                    $"Response time spike detected: {snapshot.AverageResponseTimeMs:F0}ms vs baseline {mean:F0}ms (2-sigma: {mean + 2 * stdDev:F0}ms).",
                    snapshot.AverageResponseTimeMs,
                    mean + 2 * stdDev,
                    "Correlate with deployment events or traffic pattern changes. Check for GC pauses or thread pool starvation."));
            }
        }
    }

    private void RecordSnapshot(ServiceHealthSnapshot snapshot)
    {
        lock (_lock)
        {
            if (!_history.ContainsKey(snapshot.ServiceName))
                _history[snapshot.ServiceName] = new List<ServiceHealthSnapshot>();

            _history[snapshot.ServiceName].Add(snapshot);

            if (_history[snapshot.ServiceName].Count > 100)
                _history[snapshot.ServiceName].RemoveAt(0);
        }
    }
}
