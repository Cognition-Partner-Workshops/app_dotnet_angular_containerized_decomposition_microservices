using KnowledgeAgent.Core.Interfaces;
using KnowledgeAgent.Infrastructure.BackgroundJobs;
using KnowledgeAgent.Infrastructure.Configuration;
using KnowledgeAgent.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeAgent.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddKnowledgeAgentInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        // Register configuration options
        services.Configure<SharePointOptions>(configuration.GetSection(SharePointOptions.SectionName));
        services.Configure<VectorStoreOptions>(configuration.GetSection(VectorStoreOptions.SectionName));
        services.Configure<OpenAIOptions>(configuration.GetSection(OpenAIOptions.SectionName));
        services.Configure<AppInsightsOptions>(configuration.GetSection(AppInsightsOptions.SectionName));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));

        // Register services
        services.AddScoped<ISharePointService, SharePointService>();
        services.AddScoped<IDocumentProcessor, DocumentProcessor>();
        services.AddScoped<IEmbeddingService, EmbeddingService>();
        services.AddSingleton<IVectorStoreService, QdrantVectorStoreService>();
        services.AddScoped<IKnowledgeArticleService, KnowledgeArticleService>();
        services.AddScoped<IAppInsightsService, AppInsightsService>();
        services.AddScoped<IEmailNotificationService, EmailNotificationService>();
        services.AddScoped<IKnowledgeAgentOrchestrator, KnowledgeAgentOrchestrator>();

        // Register HTTP client for Application Insights API
        services.AddHttpClient("AppInsights");

        // Register background jobs
        services.AddHostedService<SharePointSyncJob>();
        services.AddHostedService<AlertMonitoringJob>();

        return services;
    }
}
