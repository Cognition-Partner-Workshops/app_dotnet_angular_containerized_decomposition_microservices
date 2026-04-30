using AIMonitoringAgent.Models;
using AIMonitoringAgent.Services;
using Microsoft.AspNetCore.Mvc;

namespace AIMonitoringAgent.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MonitoringController : ControllerBase
{
    private readonly IAIModelMonitoringService _modelMonitoring;
    private readonly IServiceHealthMonitor _healthMonitor;
    private readonly IAnomalyDetectionService _anomalyDetection;
    private readonly IAppInsightsTelemetryService _telemetryService;

    public MonitoringController(
        IAIModelMonitoringService modelMonitoring,
        IServiceHealthMonitor healthMonitor,
        IAnomalyDetectionService anomalyDetection,
        IAppInsightsTelemetryService telemetryService)
    {
        _modelMonitoring = modelMonitoring;
        _healthMonitor = healthMonitor;
        _anomalyDetection = anomalyDetection;
        _telemetryService = telemetryService;
    }

    [HttpPost("ai-model/track")]
    public IActionResult TrackAIModelInvocation([FromBody] AIModelMetrics metrics)
    {
        _modelMonitoring.RecordInvocation(metrics);
        return Accepted(new { message = "AI model invocation tracked successfully" });
    }

    [HttpGet("ai-model/summary")]
    public IActionResult GetAllModelSummaries()
    {
        var summaries = _modelMonitoring.GetAllModelSummaries();
        return Ok(summaries);
    }

    [HttpGet("ai-model/summary/{modelName}")]
    public IActionResult GetModelSummary(string modelName)
    {
        var summary = _modelMonitoring.GetModelSummary(modelName);
        return Ok(summary);
    }

    [HttpGet("ai-model/invocations")]
    public IActionResult GetRecentInvocations([FromQuery] string? modelName = null, [FromQuery] int count = 50)
    {
        var invocations = _modelMonitoring.GetRecentInvocations(modelName, count);
        return Ok(invocations);
    }

    [HttpGet("health/services")]
    public IActionResult GetServiceHealthStatuses()
    {
        var statuses = _healthMonitor.GetLatestStatuses();
        return Ok(statuses);
    }

    [HttpPost("health/check")]
    public async Task<IActionResult> TriggerHealthCheck()
    {
        var results = await _healthMonitor.CheckAllServicesAsync();
        return Ok(results);
    }

    [HttpPost("health/check/{serviceName}")]
    public async Task<IActionResult> CheckServiceHealth(string serviceName, [FromQuery] string endpoint)
    {
        var result = await _healthMonitor.CheckServiceHealthAsync(serviceName, endpoint);
        return Ok(result);
    }

    [HttpGet("anomalies")]
    public IActionResult GetRecentAnomalies([FromQuery] int count = 50)
    {
        var anomalies = _anomalyDetection.GetRecentAnomalies(count);
        return Ok(anomalies);
    }

    [HttpPost("metrics/custom")]
    public IActionResult TrackCustomMetric([FromBody] CustomMetricRequest request)
    {
        _telemetryService.TrackCustomMetric(request.Name, request.Value, request.Properties);
        return Accepted(new { message = "Custom metric tracked" });
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var modelSummaries = _modelMonitoring.GetAllModelSummaries();
        var serviceStatuses = _healthMonitor.GetLatestStatuses();
        var recentAnomalies = _anomalyDetection.GetRecentAnomalies(10);

        return Ok(new
        {
            timestamp = DateTime.UtcNow,
            aiModels = modelSummaries,
            services = serviceStatuses,
            recentAnomalies = recentAnomalies
        });
    }
}

public class CustomMetricRequest
{
    public string Name { get; set; } = string.Empty;
    public double Value { get; set; }
    public Dictionary<string, string>? Properties { get; set; }
}
