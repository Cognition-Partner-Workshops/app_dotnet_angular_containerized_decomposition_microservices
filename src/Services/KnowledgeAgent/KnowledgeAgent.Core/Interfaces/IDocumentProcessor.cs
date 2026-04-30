using KnowledgeAgent.Core.Models;

namespace KnowledgeAgent.Core.Interfaces;

public interface IDocumentProcessor
{
    Task<string> ExtractTextAsync(Stream documentStream, string contentType, CancellationToken cancellationToken = default);
    IEnumerable<DocumentChunk> ChunkDocument(string text, Guid knowledgeArticleId, int maxChunkSize = 1000, int overlapSize = 200);
}
