using KnowledgeAgent.Core.Interfaces;
using KnowledgeAgent.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KnowledgeAgent.Infrastructure.BackgroundJobs;

public class AlertMonitoringJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AppInsightsOptions _options;
    private readonly ILogger<AlertMonitoringJob> _logger;

    public AlertMonitoringJob(
        IServiceScopeFactory scopeFactory,
        IOptions<AppInsightsOptions> options,
        ILogger<AlertMonitoringJob> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Alert monitoring job started. Interval: {Interval} minutes",
            _options.MonitoringIntervalMinutes);

        // Initial delay to let the application start up
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var orchestrator = scope.ServiceProvider.GetRequiredService<IKnowledgeAgentOrchestrator>();

                await orchestrator.RunMonitoringCycleAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in alert monitoring job");
            }

            await Task.Delay(TimeSpan.FromMinutes(_options.MonitoringIntervalMinutes), stoppingToken);
        }
    }
}
