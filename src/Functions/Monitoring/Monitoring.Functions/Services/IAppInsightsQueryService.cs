using Monitoring.Functions.Models;

namespace Monitoring.Functions.Services;

public interface IAppInsightsQueryService
{
    Task<ServiceTelemetrySummary> GetServiceTelemetryAsync(
        string serviceName, TimeSpan period, CancellationToken cancellationToken = default);

    Task<List<ServiceTelemetrySummary>> GetAllServicesTelemetryAsync(
        TimeSpan period, CancellationToken cancellationToken = default);

    Task<List<ExceptionEntry>> GetTopExceptionsAsync(
        string serviceName, TimeSpan period, int top = 10, CancellationToken cancellationToken = default);

    Task<List<TimeSeriesDataPoint>> GetRequestRateTimeSeriesAsync(
        string serviceName, TimeSpan period, TimeSpan interval, CancellationToken cancellationToken = default);

    Task<List<TimeSeriesDataPoint>> GetResponseTimeTimeSeriesAsync(
        string serviceName, TimeSpan period, TimeSpan interval, CancellationToken cancellationToken = default);

    Task<List<DependencyMetric>> GetDependencyMetricsAsync(
        string serviceName, TimeSpan period, CancellationToken cancellationToken = default);
}
