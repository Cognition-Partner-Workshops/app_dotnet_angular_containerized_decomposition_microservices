namespace KnowledgeAgent.Infrastructure.Configuration;

public class SharePointOptions
{
    public const string SectionName = "SharePoint";

    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string SiteId { get; set; } = string.Empty;
    public string DriveId { get; set; } = string.Empty;
    public string FolderPath { get; set; } = string.Empty;
    public int SyncIntervalMinutes { get; set; } = 60;
}
