using KnowledgeAgent.Core.Models;

namespace KnowledgeAgent.Core.Interfaces;

public interface IKnowledgeArticleService
{
    Task<KnowledgeArticle> CreateFromDocumentAsync(SharePointDocument document, Stream content, CancellationToken cancellationToken = default);
    Task<IEnumerable<SearchResult>> SearchKnowledgeAsync(string query, int topK = 5, CancellationToken cancellationToken = default);
    Task<KnowledgeArticle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<KnowledgeArticle>> GetAllAsync(CancellationToken cancellationToken = default);
    Task ProcessAndIndexDocumentAsync(SharePointDocument document, Stream content, CancellationToken cancellationToken = default);
}
