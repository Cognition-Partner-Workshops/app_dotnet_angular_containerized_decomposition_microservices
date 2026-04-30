using KnowledgeAgent.API.DTOs;
using KnowledgeAgent.Core.Interfaces;
using KnowledgeAgent.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeAgent.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AgentController : ControllerBase
{
    private readonly IKnowledgeAgentOrchestrator _orchestrator;
    private readonly ILogger<AgentController> _logger;

    public AgentController(
        IKnowledgeAgentOrchestrator orchestrator,
        ILogger<AgentController> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    [HttpPost("sync")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public Task<IActionResult> TriggerSync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Manual SharePoint sync triggered");

        // Run sync in background, return immediately
        _ = Task.Run(async () =>
        {
            try
            {
                await _orchestrator.SyncSharePointDocumentsAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during manual sync");
            }
        }, cancellationToken);

        return Task.FromResult<IActionResult>(Accepted(new { message = "SharePoint document sync initiated" }));
    }

    [HttpPost("monitor")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public Task<IActionResult> TriggerMonitoring(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Manual monitoring cycle triggered");

        _ = Task.Run(async () =>
        {
            try
            {
                await _orchestrator.RunMonitoringCycleAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during manual monitoring cycle");
            }
        }, cancellationToken);

        return Task.FromResult<IActionResult>(Accepted(new { message = "Monitoring cycle initiated" }));
    }

    [HttpPost("alerts/webhook")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> HandleAlertWebhook(
        [FromBody] AlertWebhookRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received alert webhook: {AlertId} - {AlertName}",
            request.AlertId, request.AlertName);

        if (string.IsNullOrWhiteSpace(request.AlertId))
            return BadRequest("AlertId is required");

        var alert = new AppInsightAlert
        {
            AlertId = request.AlertId,
            AlertName = request.AlertName,
            Severity = request.Severity,
            Description = request.Description,
            AffectedResource = request.AffectedResource,
            ExceptionType = request.ExceptionType,
            ExceptionMessage = request.ExceptionMessage,
            StackTrace = request.StackTrace,
            FiredAt = DateTime.UtcNow,
            CustomProperties = request.CustomProperties ?? new Dictionary<string, string>()
        };

        await _orchestrator.ProcessAlertAsync(alert, cancellationToken);

        return Ok(new { message = "Alert processed successfully", alertId = alert.AlertId });
    }
}
