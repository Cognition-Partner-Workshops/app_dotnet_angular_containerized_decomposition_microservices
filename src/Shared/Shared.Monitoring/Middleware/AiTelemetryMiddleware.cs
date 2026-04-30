using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shared.Monitoring.Metrics;

namespace Shared.Monitoring.Middleware;

/// <summary>
/// Captures per-request telemetry and feeds it into the ServiceMetricsCollector
/// for real-time AI health analysis.
/// </summary>
public class AiTelemetryMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ServiceMetricsCollector _metricsCollector;
    private readonly ILogger<AiTelemetryMiddleware> _logger;

    public AiTelemetryMiddleware(
        RequestDelegate next,
        ServiceMetricsCollector metricsCollector,
        ILogger<AiTelemetryMiddleware> logger)
    {
        _next = next;
        _metricsCollector = metricsCollector;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var endpoint = $"{context.Request.Method} {context.Request.Path}";

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception on {Endpoint}", endpoint);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            var statusCode = context.Response.StatusCode;
            var success = statusCode < 400;

            _metricsCollector.TrackRequest(
                endpoint,
                stopwatch.Elapsed.TotalMilliseconds,
                success,
                statusCode);
        }
    }
}
