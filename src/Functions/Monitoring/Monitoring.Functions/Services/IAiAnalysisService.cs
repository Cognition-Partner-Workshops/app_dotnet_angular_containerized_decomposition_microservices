using Monitoring.Functions.Models;

namespace Monitoring.Functions.Services;

public interface IAiAnalysisService
{
    Task<List<AnomalyResult>> DetectAnomaliesAsync(
        ServiceTelemetrySummary telemetry, CancellationToken cancellationToken = default);

    Task<string> GenerateHealthInsightAsync(
        ServiceTelemetrySummary telemetry, CancellationToken cancellationToken = default);

    Task<string> GeneratePlatformSummaryAsync(
        List<ServiceTelemetrySummary> allTelemetry, CancellationToken cancellationToken = default);

    Task<string> AnalyzeExceptionPatternAsync(
        List<ExceptionEntry> exceptions, string serviceName, CancellationToken cancellationToken = default);
}
