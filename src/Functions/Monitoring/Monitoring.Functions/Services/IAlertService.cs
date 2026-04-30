using Monitoring.Functions.Models;

namespace Monitoring.Functions.Services;

public interface IAlertService
{
    Task<MonitoringAlert> CreateAlertAsync(
        string serviceName,
        AlertLevel level,
        string title,
        string summary,
        List<AnomalyResult> anomalies,
        CancellationToken cancellationToken = default);

    Task SendAlertNotificationAsync(
        MonitoringAlert alert, CancellationToken cancellationToken = default);

    Task SendHealthReportNotificationAsync(
        HealthReport report, CancellationToken cancellationToken = default);
}
