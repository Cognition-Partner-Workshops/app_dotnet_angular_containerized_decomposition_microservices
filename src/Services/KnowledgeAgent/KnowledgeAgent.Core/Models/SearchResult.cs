namespace KnowledgeAgent.Core.Models;

public class SearchResult
{
    public Guid KnowledgeArticleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public float Score { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}
