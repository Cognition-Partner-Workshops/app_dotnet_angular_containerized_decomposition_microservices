using System.Collections.Concurrent;
using AIMonitoringAgent.Models;

namespace AIMonitoringAgent.Services;

public interface IAIModelMonitoringService
{
    void RecordInvocation(AIModelMetrics metrics);
    AIModelSummary GetModelSummary(string modelName);
    IReadOnlyList<AIModelSummary> GetAllModelSummaries();
    IReadOnlyList<AIModelMetrics> GetRecentInvocations(string? modelName = null, int count = 50);
}

public class AIModelMonitoringService : IAIModelMonitoringService
{
    private readonly IAppInsightsTelemetryService _telemetryService;
    private readonly ILogger<AIModelMonitoringService> _logger;
    private readonly ConcurrentDictionary<string, ModelTracker> _modelTrackers = new();
    private readonly ConcurrentQueue<AIModelMetrics> _recentInvocations = new();
    private const int MaxRecentInvocations = 1000;

    public AIModelMonitoringService(
        IAppInsightsTelemetryService telemetryService,
        ILogger<AIModelMonitoringService> logger)
    {
        _telemetryService = telemetryService;
        _logger = logger;
    }

    public void RecordInvocation(AIModelMetrics metrics)
    {
        _telemetryService.TrackAIModelInvocation(metrics);

        var tracker = _modelTrackers.GetOrAdd(metrics.ModelName, _ => new ModelTracker());
        tracker.Record(metrics);

        _recentInvocations.Enqueue(metrics);
        while (_recentInvocations.Count > MaxRecentInvocations)
            _recentInvocations.TryDequeue(out _);

        _logger.LogInformation(
            "AI model invocation recorded: {Model} v{Version}, Latency={Latency}ms, Success={Success}",
            metrics.ModelName, metrics.ModelVersion, metrics.Latency, metrics.IsSuccessful);
    }

    public AIModelSummary GetModelSummary(string modelName)
    {
        if (_modelTrackers.TryGetValue(modelName, out var tracker))
            return tracker.GetSummary(modelName);

        return new AIModelSummary { ModelName = modelName };
    }

    public IReadOnlyList<AIModelSummary> GetAllModelSummaries()
    {
        return _modelTrackers
            .Select(kvp => kvp.Value.GetSummary(kvp.Key))
            .OrderBy(s => s.ModelName)
            .ToList();
    }

    public IReadOnlyList<AIModelMetrics> GetRecentInvocations(string? modelName = null, int count = 50)
    {
        var query = _recentInvocations.AsEnumerable();

        if (!string.IsNullOrEmpty(modelName))
            query = query.Where(m => m.ModelName.Equals(modelName, StringComparison.OrdinalIgnoreCase));

        return query.OrderByDescending(m => m.Timestamp).Take(count).ToList();
    }

    private class ModelTracker
    {
        private long _totalInvocations;
        private long _successCount;
        private long _failureCount;
        private double _totalLatency;
        private double _totalTokens;
        private double _minLatency = double.MaxValue;
        private double _maxLatency = double.MinValue;
        private readonly object _lock = new();

        public void Record(AIModelMetrics metrics)
        {
            lock (_lock)
            {
                _totalInvocations++;
                _totalLatency += metrics.Latency;
                _totalTokens += metrics.TokensUsed;

                if (metrics.Latency < _minLatency) _minLatency = metrics.Latency;
                if (metrics.Latency > _maxLatency) _maxLatency = metrics.Latency;

                if (metrics.IsSuccessful)
                    _successCount++;
                else
                    _failureCount++;
            }
        }

        public AIModelSummary GetSummary(string modelName)
        {
            lock (_lock)
            {
                return new AIModelSummary
                {
                    ModelName = modelName,
                    TotalInvocations = _totalInvocations,
                    SuccessCount = _successCount,
                    FailureCount = _failureCount,
                    SuccessRate = _totalInvocations > 0 ? (double)_successCount / _totalInvocations * 100 : 0,
                    AverageLatencyMs = _totalInvocations > 0 ? _totalLatency / _totalInvocations : 0,
                    MinLatencyMs = _minLatency == double.MaxValue ? 0 : _minLatency,
                    MaxLatencyMs = _maxLatency == double.MinValue ? 0 : _maxLatency,
                    TotalTokensUsed = _totalTokens
                };
            }
        }
    }
}

public class AIModelSummary
{
    public string ModelName { get; set; } = string.Empty;
    public long TotalInvocations { get; set; }
    public long SuccessCount { get; set; }
    public long FailureCount { get; set; }
    public double SuccessRate { get; set; }
    public double AverageLatencyMs { get; set; }
    public double MinLatencyMs { get; set; }
    public double MaxLatencyMs { get; set; }
    public double TotalTokensUsed { get; set; }
}
