using KnowledgeAgent.Core.Interfaces;
using KnowledgeAgent.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KnowledgeAgent.Infrastructure.BackgroundJobs;

public class SharePointSyncJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SharePointOptions _options;
    private readonly ILogger<SharePointSyncJob> _logger;

    public SharePointSyncJob(
        IServiceScopeFactory scopeFactory,
        IOptions<SharePointOptions> options,
        ILogger<SharePointSyncJob> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SharePoint sync job started. Interval: {Interval} minutes",
            _options.SyncIntervalMinutes);

        // Initial delay to let the application start up
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var orchestrator = scope.ServiceProvider.GetRequiredService<IKnowledgeAgentOrchestrator>();

                await orchestrator.SyncSharePointDocumentsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SharePoint sync job");
            }

            await Task.Delay(TimeSpan.FromMinutes(_options.SyncIntervalMinutes), stoppingToken);
        }
    }
}
