using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Monitoring.HealthChecks;
using Shared.Monitoring.Metrics;
using Shared.Monitoring.Middleware;
using Shared.Monitoring.Telemetry;

namespace Shared.Monitoring.Extensions;

/// <summary>
/// Extension methods to register Application Insights AI monitoring
/// across all microservices with a single call.
/// </summary>
public static class AppInsightsServiceExtensions
{
    public static IServiceCollection AddAppInsightsMonitoring(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName)
    {
        var config = new AppInsightsConfig();
        configuration.GetSection(AppInsightsConfig.SectionName).Bind(config);

        if (!string.IsNullOrEmpty(config.ConnectionString))
        {
            services.AddApplicationInsightsTelemetry(options =>
            {
                options.ConnectionString = config.ConnectionString;
                options.EnableAdaptiveSampling = config.EnableAdaptiveSampling;
            });
        }
        else
        {
            services.AddApplicationInsightsTelemetry();
        }

        services.AddSingleton<ITelemetryInitializer>(
            new ServiceTelemetryInitializer(
                string.IsNullOrEmpty(config.CloudRoleName)
                    ? serviceName
                    : config.CloudRoleName));

        services.AddHttpContextAccessor();
        services.AddSingleton<ITelemetryInitializer, CorrelationTelemetryInitializer>();

        services.AddApplicationInsightsTelemetryProcessor<AiDiagnosticTelemetryProcessor>();

        services.AddSingleton(sp =>
        {
            var client = sp.GetRequiredService<Microsoft.ApplicationInsights.TelemetryClient>();
            return new ServiceMetricsCollector(client, serviceName);
        });

        services.AddHealthChecks()
            .AddCheck<AppInsightsHealthCheck>("app-insights");

        return services;
    }

    public static IApplicationBuilder UseAppInsightsMonitoring(this IApplicationBuilder app)
    {
        app.UseMiddleware<AiTelemetryMiddleware>();
        return app;
    }
}
