using System.Text;
using System.Text.Json;
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monitoring.Functions.Configuration;
using Monitoring.Functions.Models;
using OpenAI.Chat;

namespace Monitoring.Functions.Services;

public sealed class AiAnalysisService : IAiAnalysisService
{
    private readonly MonitoringOptions _options;
    private readonly ILogger<AiAnalysisService> _logger;
    private readonly ChatClient _chatClient;

    public AiAnalysisService(
        IOptions<MonitoringOptions> options,
        ILogger<AiAnalysisService> logger)
    {
        _options = options.Value;
        _logger = logger;

        var azureClient = new AzureOpenAIClient(
            new Uri(_options.AzureOpenAIEndpoint),
            new DefaultAzureCredential());

        _chatClient = azureClient.GetChatClient(_options.AzureOpenAIDeployment);
    }

    public async Task<List<AnomalyResult>> DetectAnomaliesAsync(
        ServiceTelemetrySummary telemetry, CancellationToken cancellationToken = default)
    {
        var anomalies = new List<AnomalyResult>();

        // Rule-based detection first
        anomalies.AddRange(DetectRuleBasedAnomalies(telemetry));

        // Statistical anomaly detection on time series
        anomalies.AddRange(DetectTimeSeriesAnomalies(telemetry));

        // AI-enhanced analysis for detected anomalies
        if (anomalies.Count > 0)
        {
            anomalies = await EnrichAnomaliesWithAiAsync(anomalies, telemetry, cancellationToken);
        }

        return anomalies;
    }

    public async Task<string> GenerateHealthInsightAsync(
        ServiceTelemetrySummary telemetry, CancellationToken cancellationToken = default)
    {
        var prompt = BuildHealthInsightPrompt(telemetry);

        try
        {
            var completion = await _chatClient.CompleteChatAsync(
                [
                    new SystemChatMessage(
                        """
                        You are an AI monitoring agent for a microservices platform.
                        Analyze the telemetry data and provide a concise health insight.
                        Focus on actionable observations. Be specific about metrics.
                        Keep the response under 200 words.
                        """),
                    new UserChatMessage(prompt)
                ],
                new ChatCompletionOptions { Temperature = 0.3f },
                cancellationToken);

            return completion.Value.Content[0].Text;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI analysis unavailable for {Service}", telemetry.ServiceName);
            return GenerateFallbackInsight(telemetry);
        }
    }

    public async Task<string> GeneratePlatformSummaryAsync(
        List<ServiceTelemetrySummary> allTelemetry, CancellationToken cancellationToken = default)
    {
        var prompt = BuildPlatformSummaryPrompt(allTelemetry);

        try
        {
            var completion = await _chatClient.CompleteChatAsync(
                [
                    new SystemChatMessage(
                        """
                        You are an AI monitoring agent for a distributed microservices platform.
                        Provide a holistic platform health summary based on all service telemetry.
                        Identify cross-service patterns, correlations, and systemic issues.
                        Prioritize critical findings. Keep the response under 300 words.
                        """),
                    new UserChatMessage(prompt)
                ],
                new ChatCompletionOptions { Temperature = 0.3f },
                cancellationToken);

            return completion.Value.Content[0].Text;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI platform summary generation failed");
            return "AI analysis unavailable. Review individual service metrics for details.";
        }
    }

    public async Task<string> AnalyzeExceptionPatternAsync(
        List<ExceptionEntry> exceptions, string serviceName, CancellationToken cancellationToken = default)
    {
        if (exceptions.Count == 0)
            return "No exceptions detected in the monitoring period.";

        var sb = new StringBuilder();
        sb.AppendLine($"Service: {serviceName}");
        sb.AppendLine("Exception patterns:");
        foreach (var ex in exceptions.Take(10))
        {
            sb.AppendLine($"  - {ex.ExceptionType}: {ex.Message} (Count: {ex.Count}, Last: {ex.LastOccurrence:u})");
        }

        try
        {
            var completion = await _chatClient.CompleteChatAsync(
                [
                    new SystemChatMessage(
                        """
                        You are an AI monitoring agent. Analyze the exception patterns and provide:
                        1. Root cause hypothesis for the most frequent exceptions
                        2. Whether exceptions are correlated
                        3. Recommended remediation steps
                        Keep the response under 200 words.
                        """),
                    new UserChatMessage(sb.ToString())
                ],
                new ChatCompletionOptions { Temperature = 0.3f },
                cancellationToken);

            return completion.Value.Content[0].Text;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI exception analysis failed for {Service}", serviceName);
            return $"Found {exceptions.Count} exception types. Top: {exceptions.First().ExceptionType} ({exceptions.First().Count} occurrences).";
        }
    }

    private List<AnomalyResult> DetectRuleBasedAnomalies(ServiceTelemetrySummary telemetry)
    {
        var anomalies = new List<AnomalyResult>();

        if (telemetry.FailureRatePercent > _options.FailureRateThresholdPercent)
        {
            anomalies.Add(new AnomalyResult(
                ServiceName: telemetry.ServiceName,
                Type: AnomalyType.HighFailureRate,
                Severity: telemetry.FailureRatePercent > 20 ? AnomalySeverity.Critical : AnomalySeverity.Warning,
                Description: $"Failure rate {telemetry.FailureRatePercent:F1}% exceeds threshold {_options.FailureRateThresholdPercent}%",
                AiAnalysis: string.Empty,
                RecommendedAction: "Investigate error logs and recent deployments",
                CurrentValue: telemetry.FailureRatePercent,
                BaselineValue: _options.FailureRateThresholdPercent,
                DeviationPercent: ((telemetry.FailureRatePercent - _options.FailureRateThresholdPercent)
                    / _options.FailureRateThresholdPercent) * 100,
                DetectedAt: DateTime.UtcNow
            ));
        }

        if (telemetry.P95ResponseTimeMs > _options.ResponseTimeThresholdMs)
        {
            anomalies.Add(new AnomalyResult(
                ServiceName: telemetry.ServiceName,
                Type: AnomalyType.ResponseTimeSpike,
                Severity: telemetry.P95ResponseTimeMs > _options.ResponseTimeThresholdMs * 3
                    ? AnomalySeverity.Critical : AnomalySeverity.Warning,
                Description: $"P95 response time {telemetry.P95ResponseTimeMs:F0}ms exceeds threshold {_options.ResponseTimeThresholdMs}ms",
                AiAnalysis: string.Empty,
                RecommendedAction: "Check dependency latency, database queries, and resource utilization",
                CurrentValue: telemetry.P95ResponseTimeMs,
                BaselineValue: _options.ResponseTimeThresholdMs,
                DeviationPercent: ((telemetry.P95ResponseTimeMs - _options.ResponseTimeThresholdMs)
                    / _options.ResponseTimeThresholdMs) * 100,
                DetectedAt: DateTime.UtcNow
            ));
        }

        if (telemetry.UniqueExceptionTypes > 5)
        {
            anomalies.Add(new AnomalyResult(
                ServiceName: telemetry.ServiceName,
                Type: AnomalyType.ExceptionBurst,
                Severity: telemetry.UniqueExceptionTypes > 15 ? AnomalySeverity.Critical : AnomalySeverity.Warning,
                Description: $"High number of unique exception types: {telemetry.UniqueExceptionTypes}",
                AiAnalysis: string.Empty,
                RecommendedAction: "Review exception patterns for systemic issues",
                CurrentValue: telemetry.UniqueExceptionTypes,
                BaselineValue: 5,
                DeviationPercent: ((telemetry.UniqueExceptionTypes - 5.0) / 5.0) * 100,
                DetectedAt: DateTime.UtcNow
            ));
        }

        foreach (var dep in telemetry.DependencyMetrics.Where(d => d.FailureRatePercent > 10))
        {
            anomalies.Add(new AnomalyResult(
                ServiceName: telemetry.ServiceName,
                Type: AnomalyType.DependencyDegradation,
                Severity: dep.FailureRatePercent > 50 ? AnomalySeverity.Critical : AnomalySeverity.Warning,
                Description: $"Dependency '{dep.DependencyName}' ({dep.DependencyType}) failure rate: {dep.FailureRatePercent:F1}%",
                AiAnalysis: string.Empty,
                RecommendedAction: $"Check health of dependency '{dep.DependencyName}'",
                CurrentValue: dep.FailureRatePercent,
                BaselineValue: 10,
                DeviationPercent: ((dep.FailureRatePercent - 10.0) / 10.0) * 100,
                DetectedAt: DateTime.UtcNow
            ));
        }

        return anomalies;
    }

    private static List<AnomalyResult> DetectTimeSeriesAnomalies(ServiceTelemetrySummary telemetry)
    {
        var anomalies = new List<AnomalyResult>();

        anomalies.AddRange(DetectZScoreAnomalies(
            telemetry.RequestTimeSeries, telemetry.ServiceName, AnomalyType.TrafficAnomaly, "request rate"));

        anomalies.AddRange(DetectZScoreAnomalies(
            telemetry.ResponseTimeTimeSeries, telemetry.ServiceName, AnomalyType.ResponseTimeSpike, "response time"));

        return anomalies;
    }

    private static List<AnomalyResult> DetectZScoreAnomalies(
        List<TimeSeriesDataPoint> timeSeries,
        string serviceName,
        AnomalyType anomalyType,
        string metricName)
    {
        var anomalies = new List<AnomalyResult>();

        if (timeSeries.Count < 5)
            return anomalies;

        var values = timeSeries.Select(p => p.Value).ToList();
        var mean = values.Average();
        var stdDev = Math.Sqrt(values.Average(v => Math.Pow(v - mean, 2)));

        if (stdDev < 0.001)
            return anomalies;

        const double zScoreThreshold = 2.5;

        foreach (var point in timeSeries.TakeLast(3))
        {
            var zScore = (point.Value - mean) / stdDev;
            if (Math.Abs(zScore) > zScoreThreshold)
            {
                anomalies.Add(new AnomalyResult(
                    ServiceName: serviceName,
                    Type: anomalyType,
                    Severity: Math.Abs(zScore) > 3.5 ? AnomalySeverity.Critical : AnomalySeverity.Warning,
                    Description: $"Statistical anomaly in {metricName}: value {point.Value:F1} at {point.Timestamp:u} " +
                                 $"(z-score: {zScore:F2}, mean: {mean:F1}, stddev: {stdDev:F1})",
                    AiAnalysis: string.Empty,
                    RecommendedAction: $"Investigate {metricName} deviation from baseline",
                    CurrentValue: point.Value,
                    BaselineValue: mean,
                    DeviationPercent: mean > 0 ? ((point.Value - mean) / mean) * 100 : 0,
                    DetectedAt: DateTime.UtcNow
                ));
            }
        }

        return anomalies;
    }

    private async Task<List<AnomalyResult>> EnrichAnomaliesWithAiAsync(
        List<AnomalyResult> anomalies,
        ServiceTelemetrySummary telemetry,
        CancellationToken cancellationToken)
    {
        var prompt = BuildAnomalyAnalysisPrompt(anomalies, telemetry);

        try
        {
            var completion = await _chatClient.CompleteChatAsync(
                [
                    new SystemChatMessage(
                        """
                        You are an AI monitoring agent. Analyze the detected anomalies in context of the
                        service telemetry. For each anomaly, provide a brief root cause analysis and
                        specific remediation steps. Return a JSON array with objects containing:
                        {"index": 0, "analysis": "...", "action": "..."}
                        Return ONLY the JSON array, no markdown.
                        """),
                    new UserChatMessage(prompt)
                ],
                new ChatCompletionOptions { Temperature = 0.2f },
                cancellationToken);

            var aiResponse = completion.Value.Content[0].Text;
            var enrichments = JsonSerializer.Deserialize<List<AnomalyEnrichment>>(aiResponse,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (enrichments is not null)
            {
                return anomalies.Select((anomaly, index) =>
                {
                    var enrichment = enrichments.FirstOrDefault(e => e.Index == index);
                    return enrichment is not null
                        ? anomaly with
                        {
                            AiAnalysis = enrichment.Analysis,
                            RecommendedAction = enrichment.Action
                        }
                        : anomaly;
                }).ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI anomaly enrichment failed for {Service}", telemetry.ServiceName);
        }

        return anomalies;
    }

    private static string BuildHealthInsightPrompt(ServiceTelemetrySummary telemetry)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Service: {telemetry.ServiceName}");
        sb.AppendLine($"Period: {telemetry.PeriodStart:u} to {telemetry.PeriodEnd:u}");
        sb.AppendLine($"Total Requests: {telemetry.TotalRequests:N0}");
        sb.AppendLine($"Failed Requests: {telemetry.FailedRequests:N0}");
        sb.AppendLine($"Failure Rate: {telemetry.FailureRatePercent:F2}%");
        sb.AppendLine($"Avg Response Time: {telemetry.AverageResponseTimeMs:F0}ms");
        sb.AppendLine($"P95 Response Time: {telemetry.P95ResponseTimeMs:F0}ms");
        sb.AppendLine($"P99 Response Time: {telemetry.P99ResponseTimeMs:F0}ms");
        sb.AppendLine($"Unique Exception Types: {telemetry.UniqueExceptionTypes}");

        if (telemetry.TopExceptions.Count > 0)
        {
            sb.AppendLine("Top Exceptions:");
            foreach (var ex in telemetry.TopExceptions.Take(5))
                sb.AppendLine($"  - {ex.ExceptionType}: {ex.Count} occurrences");
        }

        if (telemetry.DependencyMetrics.Count > 0)
        {
            sb.AppendLine("Dependencies:");
            foreach (var dep in telemetry.DependencyMetrics.Take(5))
                sb.AppendLine($"  - {dep.DependencyName} ({dep.DependencyType}): " +
                              $"Avg {dep.AverageLatencyMs:F0}ms, Failures {dep.FailureRatePercent:F1}%");
        }

        return sb.ToString();
    }

    private static string BuildPlatformSummaryPrompt(List<ServiceTelemetrySummary> allTelemetry)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Platform Telemetry Summary:");
        sb.AppendLine($"Services Monitored: {allTelemetry.Count}");
        sb.AppendLine();

        foreach (var telemetry in allTelemetry)
        {
            sb.AppendLine($"--- {telemetry.ServiceName} ---");
            sb.AppendLine($"  Requests: {telemetry.TotalRequests:N0}, Failures: {telemetry.FailureRatePercent:F2}%");
            sb.AppendLine($"  Avg/P95/P99: {telemetry.AverageResponseTimeMs:F0}/{telemetry.P95ResponseTimeMs:F0}/{telemetry.P99ResponseTimeMs:F0} ms");
            sb.AppendLine($"  Exceptions: {telemetry.UniqueExceptionTypes} types");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string BuildAnomalyAnalysisPrompt(
        List<AnomalyResult> anomalies, ServiceTelemetrySummary telemetry)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Service: {telemetry.ServiceName}");
        sb.AppendLine($"Requests: {telemetry.TotalRequests:N0}, Failure Rate: {telemetry.FailureRatePercent:F2}%");
        sb.AppendLine($"Response Times - Avg: {telemetry.AverageResponseTimeMs:F0}ms, P95: {telemetry.P95ResponseTimeMs:F0}ms");
        sb.AppendLine();
        sb.AppendLine("Detected Anomalies:");

        for (int i = 0; i < anomalies.Count; i++)
        {
            var a = anomalies[i];
            sb.AppendLine($"  [{i}] {a.Type}: {a.Description} (Severity: {a.Severity})");
        }

        return sb.ToString();
    }

    private static string GenerateFallbackInsight(ServiceTelemetrySummary telemetry)
    {
        var status = telemetry.FailureRatePercent < 1 ? "healthy" :
                     telemetry.FailureRatePercent < 5 ? "degraded" : "unhealthy";

        return $"{telemetry.ServiceName} is {status}. " +
               $"Processed {telemetry.TotalRequests:N0} requests with {telemetry.FailureRatePercent:F2}% failure rate. " +
               $"Average response time: {telemetry.AverageResponseTimeMs:F0}ms (P95: {telemetry.P95ResponseTimeMs:F0}ms).";
    }

    private record AnomalyEnrichment(int Index, string Analysis, string Action);
}
