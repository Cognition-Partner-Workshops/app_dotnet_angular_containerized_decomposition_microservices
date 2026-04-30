using Azure.Identity;
using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monitoring.Functions.Configuration;
using Monitoring.Functions.Models;

namespace Monitoring.Functions.Services;

public sealed class AppInsightsQueryService : IAppInsightsQueryService
{
    private readonly LogsQueryClient _logsClient;
    private readonly MonitoringOptions _options;
    private readonly ILogger<AppInsightsQueryService> _logger;

    public AppInsightsQueryService(
        IOptions<MonitoringOptions> options,
        ILogger<AppInsightsQueryService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _logsClient = new LogsQueryClient(new DefaultAzureCredential());
    }

    public async Task<ServiceTelemetrySummary> GetServiceTelemetryAsync(
        string serviceName, TimeSpan period, CancellationToken cancellationToken = default)
    {
        var periodEnd = DateTime.UtcNow;
        var periodStart = periodEnd - period;

        var requestMetrics = await QueryRequestMetricsAsync(serviceName, period, cancellationToken);
        var exceptions = await GetTopExceptionsAsync(serviceName, period, 10, cancellationToken);
        var dependencies = await GetDependencyMetricsAsync(serviceName, period, cancellationToken);
        var requestTimeSeries = await GetRequestRateTimeSeriesAsync(
            serviceName, period, TimeSpan.FromMinutes(5), cancellationToken);
        var responseTimeSeries = await GetResponseTimeTimeSeriesAsync(
            serviceName, period, TimeSpan.FromMinutes(5), cancellationToken);

        return new ServiceTelemetrySummary(
            ServiceName: serviceName,
            PeriodStart: periodStart,
            PeriodEnd: periodEnd,
            TotalRequests: requestMetrics.TotalRequests,
            FailedRequests: requestMetrics.FailedRequests,
            FailureRatePercent: requestMetrics.FailureRatePercent,
            AverageResponseTimeMs: requestMetrics.AverageResponseTimeMs,
            P95ResponseTimeMs: requestMetrics.P95ResponseTimeMs,
            P99ResponseTimeMs: requestMetrics.P99ResponseTimeMs,
            UniqueExceptionTypes: exceptions.Select(e => e.ExceptionType).Distinct().Count(),
            TopExceptions: exceptions,
            DependencyMetrics: dependencies,
            RequestTimeSeries: requestTimeSeries,
            ResponseTimeTimeSeries: responseTimeSeries
        );
    }

    public async Task<List<ServiceTelemetrySummary>> GetAllServicesTelemetryAsync(
        TimeSpan period, CancellationToken cancellationToken = default)
    {
        var services = _options.GetMonitoredServiceNames();
        var tasks = services.Select(s => GetServiceTelemetryAsync(s, period, cancellationToken));
        var results = await Task.WhenAll(tasks);
        return results.ToList();
    }

    public async Task<List<ExceptionEntry>> GetTopExceptionsAsync(
        string serviceName, TimeSpan period, int top = 10, CancellationToken cancellationToken = default)
    {
        var query = $"""
            exceptions
            | where cloud_RoleName == '{serviceName}'
            | where timestamp > ago({FormatTimeSpan(period)})
            | summarize Count=count(), LastOccurrence=max(timestamp) by type, outerMessage
            | top {top} by Count desc
            """;

        try
        {
            var response = await _logsClient.QueryWorkspaceAsync(
                _options.WorkspaceId, query, new QueryTimeRange(period),
                cancellationToken: cancellationToken);

            return response.Value.Table.Rows.Select(row => new ExceptionEntry(
                ExceptionType: row.GetString("type") ?? "Unknown",
                Message: row.GetString("outerMessage") ?? "No message",
                Count: row.GetInt64("Count") ?? 0,
                LastOccurrence: row.GetDateTimeOffset("LastOccurrence")?.UtcDateTime ?? DateTime.UtcNow
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query exceptions for {Service}", serviceName);
            return [];
        }
    }

    public async Task<List<TimeSeriesDataPoint>> GetRequestRateTimeSeriesAsync(
        string serviceName, TimeSpan period, TimeSpan interval, CancellationToken cancellationToken = default)
    {
        var query = $"""
            requests
            | where cloud_RoleName == '{serviceName}'
            | where timestamp > ago({FormatTimeSpan(period)})
            | summarize RequestCount=count() by bin(timestamp, {FormatTimeSpan(interval)})
            | order by timestamp asc
            """;

        try
        {
            var response = await _logsClient.QueryWorkspaceAsync(
                _options.WorkspaceId, query, new QueryTimeRange(period),
                cancellationToken: cancellationToken);

            return response.Value.Table.Rows.Select(row => new TimeSeriesDataPoint(
                Timestamp: row.GetDateTimeOffset("timestamp")?.UtcDateTime ?? DateTime.UtcNow,
                Value: row.GetDouble("RequestCount") ?? 0
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query request rate for {Service}", serviceName);
            return [];
        }
    }

    public async Task<List<TimeSeriesDataPoint>> GetResponseTimeTimeSeriesAsync(
        string serviceName, TimeSpan period, TimeSpan interval, CancellationToken cancellationToken = default)
    {
        var query = $"""
            requests
            | where cloud_RoleName == '{serviceName}'
            | where timestamp > ago({FormatTimeSpan(period)})
            | summarize AvgDuration=avg(duration) by bin(timestamp, {FormatTimeSpan(interval)})
            | order by timestamp asc
            """;

        try
        {
            var response = await _logsClient.QueryWorkspaceAsync(
                _options.WorkspaceId, query, new QueryTimeRange(period),
                cancellationToken: cancellationToken);

            return response.Value.Table.Rows.Select(row => new TimeSeriesDataPoint(
                Timestamp: row.GetDateTimeOffset("timestamp")?.UtcDateTime ?? DateTime.UtcNow,
                Value: row.GetDouble("AvgDuration") ?? 0
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query response times for {Service}", serviceName);
            return [];
        }
    }

    public async Task<List<DependencyMetric>> GetDependencyMetricsAsync(
        string serviceName, TimeSpan period, CancellationToken cancellationToken = default)
    {
        var query = $"""
            dependencies
            | where cloud_RoleName == '{serviceName}'
            | where timestamp > ago({FormatTimeSpan(period)})
            | summarize
                AvgLatency=avg(duration),
                FailureRate=100.0 * countif(success == false) / count(),
                CallCount=count()
              by type, target
            | order by CallCount desc
            """;

        try
        {
            var response = await _logsClient.QueryWorkspaceAsync(
                _options.WorkspaceId, query, new QueryTimeRange(period),
                cancellationToken: cancellationToken);

            return response.Value.Table.Rows.Select(row => new DependencyMetric(
                DependencyType: row.GetString("type") ?? "Unknown",
                DependencyName: row.GetString("target") ?? "Unknown",
                AverageLatencyMs: row.GetDouble("AvgLatency") ?? 0,
                FailureRatePercent: row.GetDouble("FailureRate") ?? 0,
                CallCount: row.GetInt64("CallCount") ?? 0
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query dependencies for {Service}", serviceName);
            return [];
        }
    }

    private async Task<RequestMetricsSummary> QueryRequestMetricsAsync(
        string serviceName, TimeSpan period, CancellationToken cancellationToken)
    {
        var query = $"""
            requests
            | where cloud_RoleName == '{serviceName}'
            | where timestamp > ago({FormatTimeSpan(period)})
            | summarize
                TotalRequests=count(),
                FailedRequests=countif(success == false),
                AvgDuration=avg(duration),
                P95Duration=percentile(duration, 95),
                P99Duration=percentile(duration, 99)
            """;

        try
        {
            var response = await _logsClient.QueryWorkspaceAsync(
                _options.WorkspaceId, query, new QueryTimeRange(period),
                cancellationToken: cancellationToken);

            var row = response.Value.Table.Rows.FirstOrDefault();
            if (row is null)
                return new RequestMetricsSummary(0, 0, 0, 0, 0, 0);

            var total = row.GetInt64("TotalRequests") ?? 0;
            var failed = row.GetInt64("FailedRequests") ?? 0;
            var failureRate = total > 0 ? (double)failed / total * 100 : 0;

            return new RequestMetricsSummary(
                TotalRequests: total,
                FailedRequests: failed,
                FailureRatePercent: failureRate,
                AverageResponseTimeMs: row.GetDouble("AvgDuration") ?? 0,
                P95ResponseTimeMs: row.GetDouble("P95Duration") ?? 0,
                P99ResponseTimeMs: row.GetDouble("P99Duration") ?? 0
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query request metrics for {Service}", serviceName);
            return new RequestMetricsSummary(0, 0, 0, 0, 0, 0);
        }
    }

    private static string FormatTimeSpan(TimeSpan ts) =>
        ts.TotalHours >= 24 ? $"{(int)ts.TotalDays}d" :
        ts.TotalMinutes >= 60 ? $"{(int)ts.TotalHours}h" :
        $"{(int)ts.TotalMinutes}m";

    private record RequestMetricsSummary(
        long TotalRequests,
        long FailedRequests,
        double FailureRatePercent,
        double AverageResponseTimeMs,
        double P95ResponseTimeMs,
        double P99ResponseTimeMs
    );
}

internal static class LogsQueryRowExtensions
{
    public static string? GetString(this LogsTableRow row, string column)
    {
        try { return row[column]?.ToString(); }
        catch { return null; }
    }

    public static long? GetInt64(this LogsTableRow row, string column)
    {
        try { return Convert.ToInt64(row[column]); }
        catch { return null; }
    }

    public static double? GetDouble(this LogsTableRow row, string column)
    {
        try { return Convert.ToDouble(row[column]); }
        catch { return null; }
    }

    public static DateTimeOffset? GetDateTimeOffset(this LogsTableRow row, string column)
    {
        try { return (DateTimeOffset?)row[column]; }
        catch { return null; }
    }
}
