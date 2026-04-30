using System.Collections.Concurrent;
using AIMonitoringAgent.Models;

namespace AIMonitoringAgent.Services;

public interface IAnomalyDetectionService
{
    AnomalyDetectionResult Evaluate(string metricName, string serviceName, double currentValue);
    IReadOnlyList<AnomalyDetectionResult> GetRecentAnomalies(int count = 50);
    void Configure(string metricName, double sensitivityMultiplier);
}

public class AnomalyDetectionService : IAnomalyDetectionService
{
    private readonly IAppInsightsTelemetryService _telemetryService;
    private readonly ILogger<AnomalyDetectionService> _logger;
    private readonly ConcurrentDictionary<string, MetricWindow> _metricWindows = new();
    private readonly ConcurrentDictionary<string, double> _sensitivityConfig = new();
    private readonly ConcurrentQueue<AnomalyDetectionResult> _recentAnomalies = new();
    private const int MaxRecentAnomalies = 200;
    private const double DefaultSensitivityMultiplier = 2.0;

    public AnomalyDetectionService(
        IAppInsightsTelemetryService telemetryService,
        ILogger<AnomalyDetectionService> logger)
    {
        _telemetryService = telemetryService;
        _logger = logger;
    }

    public AnomalyDetectionResult Evaluate(string metricName, string serviceName, double currentValue)
    {
        var key = $"{serviceName}:{metricName}";
        var window = _metricWindows.GetOrAdd(key, _ => new MetricWindow());
        window.Add(currentValue);

        var stats = window.GetStatistics();
        var sensitivity = _sensitivityConfig.GetValueOrDefault(metricName, DefaultSensitivityMultiplier);

        var deviation = stats.StdDev > 0
            ? Math.Abs(currentValue - stats.Mean) / stats.StdDev
            : 0;

        var isAnomaly = stats.Count >= 10 && deviation > sensitivity;

        var deviationPercentage = stats.Mean != 0
            ? (currentValue - stats.Mean) / stats.Mean * 100
            : 0;

        var severity = DetermineSeverity(deviation, sensitivity);

        var result = new AnomalyDetectionResult
        {
            MetricName = metricName,
            ServiceName = serviceName,
            CurrentValue = currentValue,
            ExpectedValue = Math.Round(stats.Mean, 2),
            DeviationPercentage = Math.Round(deviationPercentage, 2),
            Severity = severity,
            IsAnomaly = isAnomaly,
            Description = isAnomaly
                ? $"{metricName} for {serviceName} is {Math.Abs(deviationPercentage):F1}% " +
                  $"{(currentValue > stats.Mean ? "above" : "below")} the expected value " +
                  $"(current: {currentValue:F2}, expected: {stats.Mean:F2})"
                : $"{metricName} for {serviceName} is within normal range"
        };

        if (isAnomaly)
        {
            _telemetryService.TrackAnomaly(result);
            _recentAnomalies.Enqueue(result);
            while (_recentAnomalies.Count > MaxRecentAnomalies)
                _recentAnomalies.TryDequeue(out _);

            _logger.LogWarning("Anomaly detected: {Description}", result.Description);
        }

        return result;
    }

    public IReadOnlyList<AnomalyDetectionResult> GetRecentAnomalies(int count = 50)
    {
        return _recentAnomalies
            .OrderByDescending(a => a.DetectedAt)
            .Take(count)
            .ToList();
    }

    public void Configure(string metricName, double sensitivityMultiplier)
    {
        _sensitivityConfig[metricName] = sensitivityMultiplier;
        _logger.LogInformation("Anomaly detection sensitivity for {Metric} set to {Sensitivity}",
            metricName, sensitivityMultiplier);
    }

    private static AnomalySeverity DetermineSeverity(double deviation, double sensitivity)
    {
        if (deviation > sensitivity * 3) return AnomalySeverity.Critical;
        if (deviation > sensitivity * 2) return AnomalySeverity.High;
        if (deviation > sensitivity * 1.5) return AnomalySeverity.Medium;
        return AnomalySeverity.Low;
    }

    private class MetricWindow
    {
        private readonly Queue<TimestampedValue> _values = new();
        private readonly object _lock = new();
        private const int WindowSize = 100;
        private static readonly TimeSpan WindowDuration = TimeSpan.FromMinutes(30);

        public void Add(double value)
        {
            lock (_lock)
            {
                var cutoff = DateTime.UtcNow - WindowDuration;
                while (_values.Count > 0 && (_values.Peek().Timestamp < cutoff || _values.Count >= WindowSize))
                    _values.Dequeue();

                _values.Enqueue(new TimestampedValue(value, DateTime.UtcNow));
            }
        }

        public (double Mean, double StdDev, int Count) GetStatistics()
        {
            lock (_lock)
            {
                if (_values.Count == 0)
                    return (0, 0, 0);

                var values = _values.Select(v => v.Value).ToArray();
                var mean = values.Average();
                var variance = values.Select(v => Math.Pow(v - mean, 2)).Average();
                var stdDev = Math.Sqrt(variance);

                return (mean, stdDev, values.Length);
            }
        }

        private record TimestampedValue(double Value, DateTime Timestamp);
    }
}
