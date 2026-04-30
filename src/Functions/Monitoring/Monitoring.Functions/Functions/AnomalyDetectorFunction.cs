using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monitoring.Functions.Configuration;
using Monitoring.Functions.Models;
using Monitoring.Functions.Services;

namespace Monitoring.Functions.Functions;

public sealed class AnomalyDetectorFunction
{
    private readonly IAppInsightsQueryService _queryService;
    private readonly IAiAnalysisService _aiService;
    private readonly IAlertService _alertService;
    private readonly MonitoringOptions _options;
    private readonly ILogger<AnomalyDetectorFunction> _logger;

    public AnomalyDetectorFunction(
        IAppInsightsQueryService queryService,
        IAiAnalysisService aiService,
        IAlertService alertService,
        IOptions<MonitoringOptions> options,
        ILogger<AnomalyDetectorFunction> logger)
    {
        _queryService = queryService;
        _aiService = aiService;
        _alertService = alertService;
        _options = options.Value;
        _logger = logger;
    }

    [Function("AnomalyDetector")]
    public async Task RunAsync(
        [TimerTrigger("0 */5 * * * *")] TimerInfo timer,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Anomaly detection cycle started at {Time}", DateTime.UtcNow);

        var monitoringPeriod = TimeSpan.FromMinutes(_options.AnomalyDetectionIntervalMinutes * 6);

        try
        {
            var allTelemetry = await _queryService.GetAllServicesTelemetryAsync(
                monitoringPeriod, cancellationToken);

            var allAnomalies = new List<AnomalyResult>();

            foreach (var telemetry in allTelemetry)
            {
                var anomalies = await _aiService.DetectAnomaliesAsync(telemetry, cancellationToken);

                if (anomalies.Count > 0)
                {
                    _logger.LogWarning(
                        "Detected {Count} anomalies for {Service}",
                        anomalies.Count, telemetry.ServiceName);

                    allAnomalies.AddRange(anomalies);

                    var maxSeverity = anomalies.Max(a => a.Severity);
                    var alertLevel = maxSeverity switch
                    {
                        AnomalySeverity.Critical => AlertLevel.Critical,
                        AnomalySeverity.Warning => AlertLevel.Warning,
                        _ => AlertLevel.Information
                    };

                    var alert = await _alertService.CreateAlertAsync(
                        telemetry.ServiceName,
                        alertLevel,
                        $"Anomalies detected in {telemetry.ServiceName}",
                        $"{anomalies.Count} anomalies detected: " +
                        string.Join(", ", anomalies.Select(a => a.Type.ToString()).Distinct()),
                        anomalies,
                        cancellationToken);

                    if (alertLevel >= AlertLevel.Warning)
                    {
                        await _alertService.SendAlertNotificationAsync(alert, cancellationToken);
                    }
                }
            }

            _logger.LogInformation(
                "Anomaly detection completed. Services: {ServiceCount}, Anomalies: {AnomalyCount}",
                allTelemetry.Count, allAnomalies.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Anomaly detection cycle failed");
            throw;
        }
    }
}
