using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Metrics;

namespace Shared.Monitoring.Metrics;

/// <summary>
/// Collects and tracks custom service metrics for AI analysis.
/// Maintains a rolling window of request/error data for real-time health scoring.
/// </summary>
public class ServiceMetricsCollector
{
    private readonly TelemetryClient _telemetryClient;
    private readonly string _serviceName;
    private readonly ConcurrentQueue<RequestMetricEntry> _recentRequests = new();
    private readonly TimeSpan _windowSize = TimeSpan.FromMinutes(5);
    private long _totalRequests;
    private long _failedRequests;
    private long _totalDependencyCalls;
    private long _failedDependencyCalls;

    public ServiceMetricsCollector(TelemetryClient telemetryClient, string serviceName)
    {
        _telemetryClient = telemetryClient;
        _serviceName = serviceName;
    }

    public void TrackRequest(string endpoint, double durationMs, bool success, int statusCode)
    {
        Interlocked.Increment(ref _totalRequests);
        if (!success)
            Interlocked.Increment(ref _failedRequests);

        var entry = new RequestMetricEntry(
            DateTime.UtcNow, endpoint, durationMs, success, statusCode);
        _recentRequests.Enqueue(entry);

        PruneOldEntries();

        _telemetryClient.GetMetric($"{_serviceName}.RequestDuration", "Endpoint")
            .TrackValue(durationMs, endpoint);
        _telemetryClient.GetMetric($"{_serviceName}.RequestCount", "StatusCode")
            .TrackValue(1, statusCode.ToString());
    }

    public void TrackDependency(string dependencyType, string target, double durationMs, bool success)
    {
        Interlocked.Increment(ref _totalDependencyCalls);
        if (!success)
            Interlocked.Increment(ref _failedDependencyCalls);

        _telemetryClient.GetMetric($"{_serviceName}.DependencyDuration", "Type")
            .TrackValue(durationMs, dependencyType);
    }

    public void TrackCustomEvent(string eventName, IDictionary<string, string>? properties = null)
    {
        _telemetryClient.TrackEvent($"{_serviceName}.{eventName}", properties);
    }

    public ServiceHealthSnapshot GetHealthSnapshot()
    {
        PruneOldEntries();
        var recentList = _recentRequests.ToArray();

        var totalRecent = recentList.Length;
        var failedRecent = recentList.Count(r => !r.Success);
        var avgDuration = totalRecent > 0
            ? recentList.Average(r => r.DurationMs)
            : 0;
        var p95Duration = totalRecent > 0
            ? CalculatePercentile(recentList.Select(r => r.DurationMs).ToArray(), 95)
            : 0;

        var process = Process.GetCurrentProcess();
        var memoryMb = process.WorkingSet64 / (1024.0 * 1024.0);
        var cpuTime = process.TotalProcessorTime;

        return new ServiceHealthSnapshot(
            ServiceName: _serviceName,
            Timestamp: DateTime.UtcNow,
            TotalRequests: Interlocked.Read(ref _totalRequests),
            FailedRequests: Interlocked.Read(ref _failedRequests),
            RecentRequestCount: totalRecent,
            RecentErrorCount: failedRecent,
            ErrorRatePercent: totalRecent > 0 ? (double)failedRecent / totalRecent * 100 : 0,
            AverageResponseTimeMs: Math.Round(avgDuration, 2),
            P95ResponseTimeMs: Math.Round(p95Duration, 2),
            MemoryUsageMb: Math.Round(memoryMb, 2),
            CpuTimeSeconds: Math.Round(cpuTime.TotalSeconds, 2),
            TotalDependencyCalls: Interlocked.Read(ref _totalDependencyCalls),
            FailedDependencyCalls: Interlocked.Read(ref _failedDependencyCalls));
    }

    private void PruneOldEntries()
    {
        var cutoff = DateTime.UtcNow - _windowSize;
        while (_recentRequests.TryPeek(out var oldest) && oldest.Timestamp < cutoff)
            _recentRequests.TryDequeue(out _);
    }

    private static double CalculatePercentile(double[] values, int percentile)
    {
        if (values.Length == 0) return 0;
        Array.Sort(values);
        var index = (int)Math.Ceiling(percentile / 100.0 * values.Length) - 1;
        return values[Math.Max(0, index)];
    }
}

public record RequestMetricEntry(
    DateTime Timestamp,
    string Endpoint,
    double DurationMs,
    bool Success,
    int StatusCode);

public record ServiceHealthSnapshot(
    string ServiceName,
    DateTime Timestamp,
    long TotalRequests,
    long FailedRequests,
    int RecentRequestCount,
    int RecentErrorCount,
    double ErrorRatePercent,
    double AverageResponseTimeMs,
    double P95ResponseTimeMs,
    double MemoryUsageMb,
    double CpuTimeSeconds,
    long TotalDependencyCalls,
    long FailedDependencyCalls);
