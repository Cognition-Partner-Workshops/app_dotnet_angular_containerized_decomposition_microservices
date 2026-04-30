using System.Collections.Concurrent;
using AppInsightsMonitoringAgent.Models;
using Microsoft.ApplicationInsights;

namespace AppInsightsMonitoringAgent.AiEngine;

/// <summary>
/// Background service that periodically collects telemetry from all
/// monitored services and publishes AI-analyzed metrics to Application Insights.
/// </summary>
public class MonitoringBackgroundService : BackgroundService
{
    private readonly ServiceHealthAggregator _aggregator;
    private readonly TelemetryClient _telemetryClient;
    private readonly ILogger<MonitoringBackgroundService> _logger;
    private readonly TimeSpan _interval;

    private static readonly ConcurrentQueue<MonitoringDashboard> RecentDashboards = new();
    private const int MaxDashboardHistory = 60;

    public MonitoringBackgroundService(
        ServiceHealthAggregator aggregator,
        TelemetryClient telemetryClient,
        IConfiguration configuration,
        ILogger<MonitoringBackgroundService> logger)
    {
        _aggregator = aggregator;
        _telemetryClient = telemetryClient;
        _logger = logger;

        var intervalSeconds = configuration
            .GetValue("ApplicationInsights:MetricCollectionIntervalSeconds", 60);
        _interval = TimeSpan.FromSeconds(intervalSeconds);
    }

    public static MonitoringDashboard? LatestDashboard =>
        RecentDashboards.TryPeek(out var dashboard) ? dashboard : null;

    public static IReadOnlyCollection<MonitoringDashboard> DashboardHistory =>
        RecentDashboards.ToArray();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AI Monitoring Agent started. Collection interval: {Interval}s",
            _interval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var dashboard = await _aggregator.GetDashboardAsync();

                RecentDashboards.Enqueue(dashboard);
                while (RecentDashboards.Count > MaxDashboardHistory)
                    RecentDashboards.TryDequeue(out _);

                PublishToAppInsights(dashboard);

                _logger.LogInformation(
                    "Monitoring cycle complete. System health: {Score}/100 ({Status}). " +
                    "Services: {Total}, Anomalies: {Anomalies}, Insights: {Insights}",
                    dashboard.SystemHealthScore,
                    dashboard.OverallStatus,
                    dashboard.Services.Count,
                    dashboard.RecentAnomalies.Count,
                    dashboard.Insights.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during monitoring cycle");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private void PublishToAppInsights(MonitoringDashboard dashboard)
    {
        _telemetryClient.GetMetric("AI.SystemHealthScore")
            .TrackValue(dashboard.SystemHealthScore);

        foreach (var service in dashboard.Services)
        {
            _telemetryClient.GetMetric("AI.ServiceHealthScore", "ServiceName")
                .TrackValue(service.HealthScore.Overall, service.ServiceName);
            _telemetryClient.GetMetric("AI.ServiceAvailability", "ServiceName")
                .TrackValue(service.HealthScore.Availability, service.ServiceName);
            _telemetryClient.GetMetric("AI.ServiceResponseTime", "ServiceName")
                .TrackValue(service.Performance.AverageResponseTimeMs, service.ServiceName);
            _telemetryClient.GetMetric("AI.ServiceErrorRate", "ServiceName")
                .TrackValue(service.Performance.ErrorRatePercent, service.ServiceName);
            _telemetryClient.GetMetric("AI.ServiceMemoryMb", "ServiceName")
                .TrackValue(service.Resources.MemoryMb, service.ServiceName);
        }

        foreach (var anomaly in dashboard.RecentAnomalies)
        {
            _telemetryClient.TrackEvent("AI.AnomalyDetected", new Dictionary<string, string>
            {
                ["ServiceName"] = anomaly.ServiceName,
                ["Severity"] = anomaly.Severity.ToString(),
                ["Category"] = anomaly.Category,
                ["Description"] = anomaly.Description
            });
        }

        foreach (var insight in dashboard.Insights)
        {
            _telemetryClient.TrackEvent("AI.InsightGenerated", new Dictionary<string, string>
            {
                ["InsightId"] = insight.InsightId,
                ["Category"] = insight.Category.ToString(),
                ["Priority"] = insight.Priority.ToString(),
                ["Title"] = insight.Title
            });
        }
    }
}
