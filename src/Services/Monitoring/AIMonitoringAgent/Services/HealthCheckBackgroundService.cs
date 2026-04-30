using AIMonitoringAgent.Configuration;
using Microsoft.Extensions.Options;

namespace AIMonitoringAgent.Services;

public class HealthCheckBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<HealthCheckBackgroundService> _logger;
    private readonly MonitoringConfiguration _config;

    public HealthCheckBackgroundService(
        IServiceProvider serviceProvider,
        IOptions<MonitoringConfiguration> config,
        ILogger<HealthCheckBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _config = config.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Health check background service started. Interval: {Interval}s",
            _config.HealthCheckIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var healthMonitor = scope.ServiceProvider.GetRequiredService<IServiceHealthMonitor>();
                var results = await healthMonitor.CheckAllServicesAsync();

                var unhealthy = results.Where(r => r.State == Models.HealthState.Unhealthy).ToList();
                if (unhealthy.Count > 0)
                {
                    _logger.LogWarning("{Count} service(s) unhealthy: {Services}",
                        unhealthy.Count, string.Join(", ", unhealthy.Select(s => s.ServiceName)));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during health check cycle");
            }

            await Task.Delay(TimeSpan.FromSeconds(_config.HealthCheckIntervalSeconds), stoppingToken);
        }
    }
}
