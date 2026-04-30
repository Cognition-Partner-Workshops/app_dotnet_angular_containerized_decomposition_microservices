namespace KnowledgeAgent.Core.Models;

public class KnowledgeArticle
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string SourceDocumentId { get; set; } = string.Empty;
    public string SourceDocumentName { get; set; } = string.Empty;
    public string SharePointSiteId { get; set; } = string.Empty;
    public string SharePointDriveId { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ArticleStatus Status { get; set; } = ArticleStatus.Draft;
}
