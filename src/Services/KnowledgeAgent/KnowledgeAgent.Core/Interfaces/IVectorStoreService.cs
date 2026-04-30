using KnowledgeAgent.Core.Models;

namespace KnowledgeAgent.Core.Interfaces;

public interface IVectorStoreService
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task UpsertChunksAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default);
    Task<IEnumerable<SearchResult>> SearchAsync(float[] queryEmbedding, int topK = 5, float scoreThreshold = 0.7f, CancellationToken cancellationToken = default);
    Task DeleteByArticleIdAsync(Guid knowledgeArticleId, CancellationToken cancellationToken = default);
}
