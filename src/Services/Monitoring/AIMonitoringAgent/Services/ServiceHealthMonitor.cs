using System.Collections.Concurrent;
using System.Diagnostics;
using AIMonitoringAgent.Configuration;
using AIMonitoringAgent.Models;
using Microsoft.Extensions.Options;

namespace AIMonitoringAgent.Services;

public interface IServiceHealthMonitor
{
    Task<ServiceHealthStatus> CheckServiceHealthAsync(string serviceName, string endpoint);
    Task<IReadOnlyList<ServiceHealthStatus>> CheckAllServicesAsync();
    IReadOnlyList<ServiceHealthStatus> GetLatestStatuses();
}

public class ServiceHealthMonitor : IServiceHealthMonitor
{
    private readonly HttpClient _httpClient;
    private readonly IAppInsightsTelemetryService _telemetryService;
    private readonly IAnomalyDetectionService _anomalyDetectionService;
    private readonly IAlertingService _alertingService;
    private readonly ILogger<ServiceHealthMonitor> _logger;
    private readonly MonitoringConfiguration _config;
    private readonly ConcurrentDictionary<string, ServiceHealthStatus> _latestStatuses = new();

    public ServiceHealthMonitor(
        HttpClient httpClient,
        IAppInsightsTelemetryService telemetryService,
        IAnomalyDetectionService anomalyDetectionService,
        IAlertingService alertingService,
        IOptions<MonitoringConfiguration> config,
        ILogger<ServiceHealthMonitor> logger)
    {
        _httpClient = httpClient;
        _telemetryService = telemetryService;
        _anomalyDetectionService = anomalyDetectionService;
        _alertingService = alertingService;
        _config = config.Value;
        _logger = logger;
    }

    public async Task<ServiceHealthStatus> CheckServiceHealthAsync(string serviceName, string endpoint)
    {
        var status = new ServiceHealthStatus
        {
            ServiceName = serviceName,
            Endpoint = endpoint
        };

        var sw = Stopwatch.StartNew();
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var response = await _httpClient.GetAsync(endpoint, cts.Token);
            sw.Stop();

            status.ResponseTimeMs = sw.Elapsed.TotalMilliseconds;
            status.HttpStatusCode = (int)response.StatusCode;
            status.State = response.IsSuccessStatusCode ? HealthState.Healthy : HealthState.Degraded;
            status.ConsecutiveFailures = 0;
        }
        catch (Exception ex)
        {
            sw.Stop();
            status.ResponseTimeMs = sw.Elapsed.TotalMilliseconds;
            status.State = HealthState.Unhealthy;
            status.ErrorDetails = ex.Message;

            if (_latestStatuses.TryGetValue(serviceName, out var previous))
                status.ConsecutiveFailures = previous.ConsecutiveFailures + 1;
            else
                status.ConsecutiveFailures = 1;

            _telemetryService.TrackException(ex, new Dictionary<string, string>
            {
                ["ServiceName"] = serviceName,
                ["Endpoint"] = endpoint
            });
        }

        _latestStatuses[serviceName] = status;
        _telemetryService.TrackServiceHealth(status);
        _anomalyDetectionService.Evaluate("ResponseTime", serviceName, status.ResponseTimeMs);
        _alertingService.EvaluateMetric("ConsecutiveFailures", serviceName, status.ConsecutiveFailures);

        return status;
    }

    public async Task<IReadOnlyList<ServiceHealthStatus>> CheckAllServicesAsync()
    {
        var tasks = _config.ServiceEndpoints
            .Select(kvp => CheckServiceHealthAsync(kvp.Key, kvp.Value))
            .ToList();

        var results = await Task.WhenAll(tasks);
        return results.ToList();
    }

    public IReadOnlyList<ServiceHealthStatus> GetLatestStatuses()
    {
        return _latestStatuses.Values
            .OrderBy(s => s.ServiceName)
            .ToList();
    }
}
