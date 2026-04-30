using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Monitoring.Functions.Configuration;
using Monitoring.Functions.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        services.Configure<MonitoringOptions>(
            context.Configuration.GetSection("Monitoring"));

        services.AddHttpClient();
        services.AddSingleton<IAppInsightsQueryService, AppInsightsQueryService>();
        services.AddSingleton<IAiAnalysisService, AiAnalysisService>();
        services.AddSingleton<IAlertService, AlertService>();
    })
    .Build();

host.Run();
