using AppInsightsMonitoringAgent.Models;

namespace AppInsightsMonitoringAgent.AiEngine;

/// <summary>
/// Generates AI-driven insights by analyzing patterns across service health
/// snapshots. Produces actionable recommendations for performance, reliability,
/// scalability, and cost optimization.
/// </summary>
public class AiInsightsEngine
{
    private readonly ILogger<AiInsightsEngine> _logger;
    private int _insightCounter;

    public AiInsightsEngine(ILogger<AiInsightsEngine> logger)
    {
        _logger = logger;
    }

    public List<AiInsight> GenerateInsights(
        Dictionary<string, ServiceHealthSnapshot> snapshots,
        Dictionary<string, List<AnomalyReport>> anomalies)
    {
        var insights = new List<AiInsight>();

        AnalyzePerformancePatterns(snapshots, insights);
        AnalyzeReliabilityPatterns(snapshots, anomalies, insights);
        AnalyzeScalabilityPatterns(snapshots, insights);
        AnalyzeCrossServicePatterns(snapshots, anomalies, insights);

        return insights;
    }

    private void AnalyzePerformancePatterns(
        Dictionary<string, ServiceHealthSnapshot> snapshots,
        List<AiInsight> insights)
    {
        var slowServices = snapshots
            .Where(s => s.Value.P95ResponseTimeMs > 1000 && s.Value.RecentRequestCount > 0)
            .Select(s => s.Key)
            .ToList();

        if (slowServices.Count > 0)
        {
            insights.Add(new AiInsight(
                GenerateInsightId(),
                DateTime.UtcNow,
                InsightCategory.Performance,
                "Slow Service Response Times Detected",
                $"{slowServices.Count} service(s) have P95 response times exceeding 1 second. " +
                "Consider enabling response caching, optimizing database queries, or implementing read replicas.",
                slowServices.Count > 2 ? InsightPriority.High : InsightPriority.Medium,
                slowServices,
                new List<string>
                {
                    "Enable Application Insights Profiler to identify hot paths",
                    "Review database query execution plans for N+1 query patterns",
                    "Consider implementing response caching for frequently accessed endpoints",
                    "Evaluate connection pooling configuration"
                }));
        }
    }

    private void AnalyzeReliabilityPatterns(
        Dictionary<string, ServiceHealthSnapshot> snapshots,
        Dictionary<string, List<AnomalyReport>> anomalies,
        List<AiInsight> insights)
    {
        var servicesWithErrors = snapshots
            .Where(s => s.Value.ErrorRatePercent > 1 && s.Value.RecentRequestCount > 0)
            .Select(s => s.Key)
            .ToList();

        if (servicesWithErrors.Count > 0)
        {
            insights.Add(new AiInsight(
                GenerateInsightId(),
                DateTime.UtcNow,
                InsightCategory.Reliability,
                "Elevated Error Rates Across Services",
                $"{servicesWithErrors.Count} service(s) are experiencing error rates above 1%. " +
                "This may indicate systemic issues with shared dependencies.",
                InsightPriority.High,
                servicesWithErrors,
                new List<string>
                {
                    "Check shared dependency health (PostgreSQL, RabbitMQ)",
                    "Review recent deployment changes for breaking modifications",
                    "Implement circuit breaker patterns for inter-service communication",
                    "Set up structured exception logging for root cause analysis"
                }));
        }

        var servicesWithDependencyIssues = snapshots
            .Where(s => s.Value.TotalDependencyCalls > 0 &&
                        (double)s.Value.FailedDependencyCalls / s.Value.TotalDependencyCalls > 0.05)
            .Select(s => s.Key)
            .ToList();

        if (servicesWithDependencyIssues.Count > 0)
        {
            insights.Add(new AiInsight(
                GenerateInsightId(),
                DateTime.UtcNow,
                InsightCategory.Reliability,
                "Dependency Failure Pattern Detected",
                "Multiple services report high dependency failure rates. " +
                "Downstream services or infrastructure may be degraded.",
                InsightPriority.Urgent,
                servicesWithDependencyIssues,
                new List<string>
                {
                    "Verify database connection pool health across all services",
                    "Check RabbitMQ broker status and queue depths",
                    "Implement retry policies with exponential backoff",
                    "Consider adding bulkhead isolation for critical dependencies"
                }));
        }
    }

    private void AnalyzeScalabilityPatterns(
        Dictionary<string, ServiceHealthSnapshot> snapshots,
        List<AiInsight> insights)
    {
        var highMemoryServices = snapshots
            .Where(s => s.Value.MemoryUsageMb > 512)
            .Select(s => s.Key)
            .ToList();

        if (highMemoryServices.Count > 0)
        {
            insights.Add(new AiInsight(
                GenerateInsightId(),
                DateTime.UtcNow,
                InsightCategory.Scalability,
                "High Memory Consumption Detected",
                $"{highMemoryServices.Count} service(s) are using more than 512MB of memory. " +
                "This may indicate memory leaks or inefficient object allocation.",
                InsightPriority.Medium,
                highMemoryServices,
                new List<string>
                {
                    "Capture memory dumps and analyze with dotnet-dump",
                    "Enable Application Insights memory profiling",
                    "Review object lifetime management and IDisposable patterns",
                    "Consider implementing object pooling for high-allocation scenarios"
                }));
        }
    }

    private void AnalyzeCrossServicePatterns(
        Dictionary<string, ServiceHealthSnapshot> snapshots,
        Dictionary<string, List<AnomalyReport>> anomalies,
        List<AiInsight> insights)
    {
        var totalAnomalies = anomalies.Values.Sum(a => a.Count);
        if (totalAnomalies > 5)
        {
            insights.Add(new AiInsight(
                GenerateInsightId(),
                DateTime.UtcNow,
                InsightCategory.Reliability,
                "System-Wide Instability Detected",
                $"{totalAnomalies} anomalies detected across {anomalies.Count} services. " +
                "This pattern suggests a cascading failure or shared infrastructure issue.",
                InsightPriority.Urgent,
                anomalies.Keys.ToList(),
                new List<string>
                {
                    "Investigate shared infrastructure components (network, DNS, load balancers)",
                    "Check for correlated deployment events across services",
                    "Review API Gateway health and routing configuration",
                    "Consider enabling distributed tracing correlation for root cause analysis"
                }));
        }
    }

    private string GenerateInsightId() =>
        $"insight-{Interlocked.Increment(ref _insightCounter):D6}-{DateTime.UtcNow:yyyyMMddHHmmss}";
}
