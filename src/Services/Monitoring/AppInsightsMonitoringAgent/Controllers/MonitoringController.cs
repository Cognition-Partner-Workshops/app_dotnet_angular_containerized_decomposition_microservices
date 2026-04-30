using AppInsightsMonitoringAgent.AiEngine;
using AppInsightsMonitoringAgent.Models;
using Microsoft.AspNetCore.Mvc;

namespace AppInsightsMonitoringAgent.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MonitoringController : ControllerBase
{
    private readonly ServiceHealthAggregator _aggregator;
    private readonly ILogger<MonitoringController> _logger;

    public MonitoringController(
        ServiceHealthAggregator aggregator,
        ILogger<MonitoringController> logger)
    {
        _aggregator = aggregator;
        _logger = logger;
    }

    /// <summary>
    /// Returns the full AI monitoring dashboard with health scores, anomalies, and insights.
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<MonitoringDashboard>> GetDashboard()
    {
        var dashboard = await _aggregator.GetDashboardAsync();
        return Ok(dashboard);
    }

    /// <summary>
    /// Returns the most recent cached dashboard snapshot without re-probing services.
    /// </summary>
    [HttpGet("dashboard/latest")]
    public ActionResult<MonitoringDashboard> GetLatestDashboard()
    {
        var dashboard = MonitoringBackgroundService.LatestDashboard;
        if (dashboard is null)
            return NotFound("No monitoring data available yet. The background service may still be initializing.");

        return Ok(dashboard);
    }

    /// <summary>
    /// Returns health score and status for all monitored services.
    /// </summary>
    [HttpGet("services")]
    public async Task<ActionResult<IEnumerable<ServiceHealthReport>>> GetServiceHealth()
    {
        var dashboard = await _aggregator.GetDashboardAsync();
        return Ok(dashboard.Services);
    }

    /// <summary>
    /// Returns health details for a specific service.
    /// </summary>
    [HttpGet("services/{serviceName}")]
    public async Task<ActionResult<ServiceHealthReport>> GetServiceHealth(string serviceName)
    {
        var dashboard = await _aggregator.GetDashboardAsync();
        var service = dashboard.Services
            .FirstOrDefault(s => s.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase));

        if (service is null)
            return NotFound($"Service '{serviceName}' not found.");

        return Ok(service);
    }

    /// <summary>
    /// Returns all active anomalies detected by the AI engine.
    /// </summary>
    [HttpGet("anomalies")]
    public async Task<ActionResult<IEnumerable<AnomalyReport>>> GetAnomalies(
        [FromQuery] AnomalySeverity? severity = null)
    {
        var dashboard = await _aggregator.GetDashboardAsync();
        var anomalies = dashboard.RecentAnomalies.AsEnumerable();

        if (severity.HasValue)
            anomalies = anomalies.Where(a => a.Severity == severity.Value);

        return Ok(anomalies);
    }

    /// <summary>
    /// Returns AI-generated insights and recommendations.
    /// </summary>
    [HttpGet("insights")]
    public async Task<ActionResult<IEnumerable<AiInsight>>> GetInsights(
        [FromQuery] InsightCategory? category = null,
        [FromQuery] InsightPriority? priority = null)
    {
        var dashboard = await _aggregator.GetDashboardAsync();
        var insights = dashboard.Insights.AsEnumerable();

        if (category.HasValue)
            insights = insights.Where(i => i.Category == category.Value);
        if (priority.HasValue)
            insights = insights.Where(i => i.Priority == priority.Value);

        return Ok(insights);
    }

    /// <summary>
    /// Returns historical dashboard snapshots for trend analysis.
    /// </summary>
    [HttpGet("history")]
    public ActionResult<IEnumerable<MonitoringDashboard>> GetHistory(
        [FromQuery] int count = 10)
    {
        var history = MonitoringBackgroundService.DashboardHistory
            .TakeLast(Math.Min(count, 60))
            .ToList();

        return Ok(history);
    }

    /// <summary>
    /// Returns system-wide health summary.
    /// </summary>
    [HttpGet("summary")]
    public async Task<ActionResult<object>> GetSummary()
    {
        var dashboard = await _aggregator.GetDashboardAsync();
        return Ok(new
        {
            dashboard.GeneratedAt,
            dashboard.OverallStatus,
            dashboard.SystemHealthScore,
            ServiceCount = dashboard.Services.Count,
            HealthyCount = dashboard.Services.Count(s => s.Status == ServiceStatus.Healthy),
            DegradedCount = dashboard.Services.Count(s => s.Status == ServiceStatus.Degraded),
            UnhealthyCount = dashboard.Services.Count(s => s.Status == ServiceStatus.Unhealthy),
            AnomalyCount = dashboard.RecentAnomalies.Count,
            CriticalAnomalyCount = dashboard.RecentAnomalies.Count(a => a.Severity == AnomalySeverity.Critical),
            InsightCount = dashboard.Insights.Count
        });
    }
}
