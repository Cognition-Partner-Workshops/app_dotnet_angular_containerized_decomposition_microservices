namespace KnowledgeAgent.Core.Models;

public class SharePointDocument
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public string WebUrl { get; set; } = string.Empty;
    public string DriveId { get; set; } = string.Empty;
    public string SiteId { get; set; } = string.Empty;
    public DateTime LastModified { get; set; }
    public string LastModifiedBy { get; set; } = string.Empty;
}
