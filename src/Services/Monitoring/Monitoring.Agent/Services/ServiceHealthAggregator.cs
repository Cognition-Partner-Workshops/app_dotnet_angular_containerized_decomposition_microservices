using System.Diagnostics;
using Monitoring.Agent.Models;
using Shared.Monitoring;
using Shared.Monitoring.Metrics;

namespace Monitoring.Agent.Services;

/// <summary>
/// Aggregates health data from all monitored microservices by polling
/// their /healthz endpoints and collecting local process metrics.
/// Produces a unified monitoring dashboard with AI-generated insights.
/// </summary>
public class ServiceHealthAggregator
{
    private readonly HttpClient _httpClient;
    private readonly AnomalyDetectionEngine _anomalyDetection;
    private readonly HealthScoringEngine _healthScoring;
    private readonly AiInsightsEngine _insightsEngine;
    private readonly ILogger<ServiceHealthAggregator> _logger;
    private readonly Dictionary<string, string> _serviceEndpoints;

    public ServiceHealthAggregator(
        IHttpClientFactory httpClientFactory,
        AnomalyDetectionEngine anomalyDetection,
        HealthScoringEngine healthScoring,
        AiInsightsEngine insightsEngine,
        IConfiguration configuration,
        ILogger<ServiceHealthAggregator> logger)
    {
        _httpClient = httpClientFactory.CreateClient("MonitoringAgent");
        _httpClient.Timeout = TimeSpan.FromSeconds(5);
        _anomalyDetection = anomalyDetection;
        _healthScoring = healthScoring;
        _insightsEngine = insightsEngine;
        _logger = logger;

        _serviceEndpoints = configuration
            .GetSection("MonitoredServices")
            .Get<Dictionary<string, string>>() ?? new Dictionary<string, string>
            {
                ["Identity"] = "http://localhost:5001",
                ["Customer"] = "http://localhost:5002",
                ["Order"] = "http://localhost:5003",
                ["Product"] = "http://localhost:5004",
                ["Notification"] = "http://localhost:5005",
                ["ApiGateway"] = "http://localhost:5000"
            };
    }

    public async Task<MonitoringDashboard> GetDashboardAsync()
    {
        var serviceReports = new List<ServiceHealthReport>();
        var allAnomalies = new Dictionary<string, List<AnomalyReport>>();
        var snapshots = new Dictionary<string, ServiceHealthSnapshot>();

        var tasks = _serviceEndpoints.Select(async kvp =>
        {
            var report = await ProbeServiceAsync(kvp.Key, kvp.Value);
            return (kvp.Key, report);
        });

        var results = await Task.WhenAll(tasks);

        foreach (var (name, report) in results)
        {
            serviceReports.Add(report.HealthReport);
            allAnomalies[name] = report.Anomalies;
            if (report.Snapshot is not null)
                snapshots[name] = report.Snapshot;
        }

        var insights = _insightsEngine.GenerateInsights(snapshots, allAnomalies);
        var recentAnomalies = allAnomalies.Values.SelectMany(a => a).ToList();
        var systemScore = serviceReports.Count > 0
            ? serviceReports.Average(r => r.HealthScore.Overall)
            : 0;

        var overallStatus = systemScore switch
        {
            >= 80 => "Healthy",
            >= 50 => "Degraded",
            _ => "Unhealthy"
        };

        return new MonitoringDashboard(
            GeneratedAt: DateTime.UtcNow,
            OverallStatus: overallStatus,
            SystemHealthScore: Math.Round(systemScore, 1),
            Services: serviceReports,
            RecentAnomalies: recentAnomalies,
            Insights: insights,
            SystemMetrics: new Dictionary<string, object>
            {
                ["TotalServices"] = _serviceEndpoints.Count,
                ["HealthyServices"] = serviceReports.Count(r => r.Status == ServiceStatus.Healthy),
                ["DegradedServices"] = serviceReports.Count(r => r.Status == ServiceStatus.Degraded),
                ["UnhealthyServices"] = serviceReports.Count(r => r.Status == ServiceStatus.Unhealthy),
                ["TotalAnomalies"] = recentAnomalies.Count,
                ["CriticalAnomalies"] = recentAnomalies.Count(a => a.Severity == AnomalySeverity.Critical)
            });
    }

    private async Task<ServiceProbeResult> ProbeServiceAsync(string serviceName, string baseUrl)
    {
        var snapshot = CreateLocalSnapshot(serviceName);
        var anomalies = _anomalyDetection.Analyze(snapshot);
        var healthScore = _healthScoring.CalculateScore(snapshot, anomalies);
        var status = _healthScoring.DetermineStatus(healthScore.Overall);

        bool isReachable;
        try
        {
            var response = await _httpClient.GetAsync($"{baseUrl}/healthz");
            isReachable = response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to reach {Service} at {Url}", serviceName, baseUrl);
            isReachable = false;
        }

        if (!isReachable)
            status = ServiceStatus.Unknown;

        var process = Process.GetCurrentProcess();
        var performance = new PerformanceMetrics(
            AverageResponseTimeMs: snapshot.AverageResponseTimeMs,
            P95ResponseTimeMs: snapshot.P95ResponseTimeMs,
            RequestsPerMinute: snapshot.RecentRequestCount,
            ErrorRatePercent: snapshot.ErrorRatePercent,
            ActiveConnections: 0);

        var gcInfo = GC.GetGCMemoryInfo();
        var resources = new ResourceUtilization(
            CpuPercent: snapshot.CpuTimeSeconds,
            MemoryMb: snapshot.MemoryUsageMb,
            MemoryPercent: snapshot.MemoryUsageMb / 1024.0 * 100,
            GcGen0Collections: GC.CollectionCount(0),
            GcGen1Collections: GC.CollectionCount(1),
            GcGen2Collections: GC.CollectionCount(2),
            GcTotalMemoryMb: Math.Round(GC.GetTotalMemory(false) / (1024.0 * 1024.0), 2));

        var recommendations = GenerateRecommendations(snapshot, anomalies, isReachable);

        var healthReport = new ServiceHealthReport(
            ServiceName: serviceName,
            Timestamp: DateTime.UtcNow,
            HealthScore: healthScore,
            Status: status,
            Performance: performance,
            Resources: resources,
            ActiveAnomalies: anomalies,
            Recommendations: recommendations);

        return new ServiceProbeResult(healthReport, anomalies, snapshot);
    }

    private static ServiceHealthSnapshot CreateLocalSnapshot(string serviceName)
    {
        var process = Process.GetCurrentProcess();
        return new ServiceHealthSnapshot(
            ServiceName: serviceName,
            Timestamp: DateTime.UtcNow,
            TotalRequests: 0,
            FailedRequests: 0,
            RecentRequestCount: 0,
            RecentErrorCount: 0,
            ErrorRatePercent: 0,
            AverageResponseTimeMs: 0,
            P95ResponseTimeMs: 0,
            MemoryUsageMb: Math.Round(process.WorkingSet64 / (1024.0 * 1024.0), 2),
            CpuTimeSeconds: Math.Round(process.TotalProcessorTime.TotalSeconds, 2),
            TotalDependencyCalls: 0,
            FailedDependencyCalls: 0);
    }

    private static List<string> GenerateRecommendations(
        ServiceHealthSnapshot snapshot,
        List<AnomalyReport> anomalies,
        bool isReachable)
    {
        var recommendations = new List<string>();

        if (!isReachable)
            recommendations.Add("Service is unreachable. Verify deployment status and network configuration.");

        if (snapshot.P95ResponseTimeMs > 1000)
            recommendations.Add("Enable Application Insights Profiler to identify performance bottlenecks.");

        if (snapshot.ErrorRatePercent > 2)
            recommendations.Add("Set up Application Insights Smart Detection alerts for anomalous error rates.");

        if (anomalies.Count > 3)
            recommendations.Add("Multiple anomalies detected. Consider scaling the service or investigating root cause.");

        if (recommendations.Count == 0)
            recommendations.Add("No immediate action required. Service is operating within normal parameters.");

        return recommendations;
    }

    private record ServiceProbeResult(
        ServiceHealthReport HealthReport,
        List<AnomalyReport> Anomalies,
        ServiceHealthSnapshot? Snapshot);
}
