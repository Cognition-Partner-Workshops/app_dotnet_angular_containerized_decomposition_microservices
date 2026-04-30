using AIMonitoringAgent.Models;
using AIMonitoringAgent.Services;
using Microsoft.AspNetCore.Mvc;

namespace AIMonitoringAgent.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AlertsController : ControllerBase
{
    private readonly IAlertingService _alertingService;

    public AlertsController(IAlertingService alertingService)
    {
        _alertingService = alertingService;
    }

    [HttpGet("rules")]
    public IActionResult GetAllRules()
    {
        var rules = _alertingService.GetAllRules();
        return Ok(rules);
    }

    [HttpGet("rules/{ruleId}")]
    public IActionResult GetRule(string ruleId)
    {
        var rule = _alertingService.GetRule(ruleId);
        if (rule == null) return NotFound();
        return Ok(rule);
    }

    [HttpPost("rules")]
    public IActionResult AddRule([FromBody] AlertRule rule)
    {
        var created = _alertingService.AddRule(rule);
        return CreatedAtAction(nameof(GetRule), new { ruleId = created.Id }, created);
    }

    [HttpDelete("rules/{ruleId}")]
    public IActionResult DeleteRule(string ruleId)
    {
        var removed = _alertingService.RemoveRule(ruleId);
        if (!removed) return NotFound();
        return NoContent();
    }

    [HttpPost("evaluate")]
    public IActionResult EvaluateMetric([FromBody] MetricEvaluationRequest request)
    {
        var notification = _alertingService.EvaluateMetric(request.MetricName, request.ServiceName, request.Value);
        if (notification == null)
            return Ok(new { triggered = false, message = "No alert rules triggered" });

        return Ok(new { triggered = true, alert = notification });
    }

    [HttpGet("recent")]
    public IActionResult GetRecentAlerts([FromQuery] int count = 50)
    {
        var alerts = _alertingService.GetRecentAlerts(count);
        return Ok(alerts);
    }
}

public class MetricEvaluationRequest
{
    public string MetricName { get; set; } = string.Empty;
    public string? ServiceName { get; set; }
    public double Value { get; set; }
}
