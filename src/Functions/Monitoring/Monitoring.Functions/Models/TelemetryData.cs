namespace Monitoring.Functions.Models;

public record TelemetryData(
    string ServiceName,
    DateTime Timestamp,
    long TotalRequests,
    long FailedRequests,
    double AverageResponseTimeMs,
    double P95ResponseTimeMs,
    double P99ResponseTimeMs,
    double FailureRatePercent,
    List<ExceptionEntry> TopExceptions,
    List<DependencyMetric> DependencyMetrics
);

public record ExceptionEntry(
    string ExceptionType,
    string Message,
    long Count,
    DateTime LastOccurrence
);

public record DependencyMetric(
    string DependencyType,
    string DependencyName,
    double AverageLatencyMs,
    double FailureRatePercent,
    long CallCount
);

public record ServiceTelemetrySummary(
    string ServiceName,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    long TotalRequests,
    long FailedRequests,
    double FailureRatePercent,
    double AverageResponseTimeMs,
    double P95ResponseTimeMs,
    double P99ResponseTimeMs,
    int UniqueExceptionTypes,
    List<ExceptionEntry> TopExceptions,
    List<DependencyMetric> DependencyMetrics,
    List<TimeSeriesDataPoint> RequestTimeSeries,
    List<TimeSeriesDataPoint> ResponseTimeTimeSeries
);

public record TimeSeriesDataPoint(
    DateTime Timestamp,
    double Value
);
