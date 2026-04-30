using AppInsightsMonitoringAgent.Models;

namespace AppInsightsMonitoringAgent.AiEngine;

/// <summary>
/// Computes a composite health score (0-100) for each service based on
/// availability, performance, error rate, and resource utilization.
/// Weights are tuned for microservice workloads.
/// </summary>
public class HealthScoringEngine
{
    private const double AvailabilityWeight = 0.30;
    private const double PerformanceWeight = 0.30;
    private const double ErrorRateWeight = 0.25;
    private const double ResourceWeight = 0.15;

    public HealthScore CalculateScore(ServiceHealthSnapshot snapshot, List<AnomalyReport> anomalies)
    {
        var availability = CalculateAvailabilityScore(snapshot);
        var performance = CalculatePerformanceScore(snapshot);
        var errorRate = CalculateErrorRateScore(snapshot);
        var resource = CalculateResourceScore(snapshot);

        var overall = availability * AvailabilityWeight
            + performance * PerformanceWeight
            + errorRate * ErrorRateWeight
            + resource * ResourceWeight;

        var anomalyPenalty = anomalies.Sum(a => a.Severity switch
        {
            AnomalySeverity.Critical => 15,
            AnomalySeverity.Warning => 5,
            _ => 0
        });

        overall = Math.Max(0, overall - anomalyPenalty);

        return new HealthScore(
            Overall: Math.Round(overall, 1),
            Availability: Math.Round(availability, 1),
            Performance: Math.Round(performance, 1),
            ErrorRate: Math.Round(errorRate, 1),
            ResourceUsage: Math.Round(resource, 1));
    }

    public ServiceStatus DetermineStatus(double overallScore) => overallScore switch
    {
        >= 80 => ServiceStatus.Healthy,
        >= 50 => ServiceStatus.Degraded,
        _ => ServiceStatus.Unhealthy
    };

    private static double CalculateAvailabilityScore(ServiceHealthSnapshot snapshot)
    {
        if (snapshot.TotalRequests == 0) return 100;
        var successRate = 1.0 - (double)snapshot.FailedRequests / snapshot.TotalRequests;
        return Math.Max(0, successRate * 100);
    }

    private static double CalculatePerformanceScore(ServiceHealthSnapshot snapshot)
    {
        if (snapshot.RecentRequestCount == 0) return 100;
        return snapshot.P95ResponseTimeMs switch
        {
            < 100 => 100,
            < 250 => 90,
            < 500 => 80,
            < 1000 => 60,
            < 2000 => 40,
            < 5000 => 20,
            _ => 5
        };
    }

    private static double CalculateErrorRateScore(ServiceHealthSnapshot snapshot)
    {
        if (snapshot.RecentRequestCount == 0) return 100;
        return snapshot.ErrorRatePercent switch
        {
            < 0.1 => 100,
            < 1 => 90,
            < 2 => 75,
            < 5 => 50,
            < 10 => 25,
            _ => 5
        };
    }

    private static double CalculateResourceScore(ServiceHealthSnapshot snapshot)
    {
        var memoryScore = snapshot.MemoryUsageMb switch
        {
            < 128 => 100,
            < 256 => 90,
            < 512 => 75,
            < 1024 => 50,
            _ => 25
        };
        return memoryScore;
    }
}
