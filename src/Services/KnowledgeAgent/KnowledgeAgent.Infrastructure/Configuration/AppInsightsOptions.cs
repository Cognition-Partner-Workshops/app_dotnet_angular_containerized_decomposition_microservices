namespace KnowledgeAgent.Infrastructure.Configuration;

public class AppInsightsOptions
{
    public const string SectionName = "AppInsights";

    public string ConnectionString { get; set; } = string.Empty;
    public string InstrumentationKey { get; set; } = string.Empty;
    public string ApplicationId { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public int MonitoringIntervalMinutes { get; set; } = 5;
    public int AlertLookbackMinutes { get; set; } = 15;
}
