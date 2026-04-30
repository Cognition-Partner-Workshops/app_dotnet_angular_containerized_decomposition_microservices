using System.Collections.Concurrent;
using KnowledgeAgent.Core.Interfaces;
using KnowledgeAgent.Core.Models;
using Microsoft.Extensions.Logging;

namespace KnowledgeAgent.Infrastructure.Services;

public class KnowledgeArticleService : IKnowledgeArticleService
{
    private readonly IDocumentProcessor _documentProcessor;
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStoreService _vectorStoreService;
    private readonly ILogger<KnowledgeArticleService> _logger;

    // In-memory store for simplicity; in production, use a database
    private static readonly ConcurrentDictionary<Guid, KnowledgeArticle> ArticleStore = new();

    public KnowledgeArticleService(
        IDocumentProcessor documentProcessor,
        IEmbeddingService embeddingService,
        IVectorStoreService vectorStoreService,
        ILogger<KnowledgeArticleService> logger)
    {
        _documentProcessor = documentProcessor;
        _embeddingService = embeddingService;
        _vectorStoreService = vectorStoreService;
        _logger = logger;
    }

    public async Task<KnowledgeArticle> CreateFromDocumentAsync(
        SharePointDocument document, Stream content, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating knowledge article from document: {DocumentName}", document.Name);

        var article = new KnowledgeArticle
        {
            Title = Path.GetFileNameWithoutExtension(document.Name),
            SourceDocumentId = document.Id,
            SourceDocumentName = document.Name,
            SharePointSiteId = document.SiteId,
            SharePointDriveId = document.DriveId,
            Status = ArticleStatus.Processing
        };

        try
        {
            var text = await _documentProcessor.ExtractTextAsync(content, document.ContentType, cancellationToken);
            article.Content = text;
            article.Summary = GenerateSummary(text);

            ArticleStore[article.Id] = article;

            await ProcessAndIndexDocumentAsync(document, content, cancellationToken);

            article.Status = ArticleStatus.Indexed;
            article.UpdatedAt = DateTime.UtcNow;
            ArticleStore[article.Id] = article;

            _logger.LogInformation("Successfully created and indexed knowledge article: {ArticleId}", article.Id);
        }
        catch (Exception ex)
        {
            article.Status = ArticleStatus.Failed;
            ArticleStore[article.Id] = article;
            _logger.LogError(ex, "Error creating knowledge article from document: {DocumentName}", document.Name);
            throw;
        }

        return article;
    }

    public async Task<IEnumerable<SearchResult>> SearchKnowledgeAsync(
        string query, int topK = 5, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Searching knowledge base for: {Query}", query);

        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query, cancellationToken);
        var results = await _vectorStoreService.SearchAsync(queryEmbedding, topK, cancellationToken: cancellationToken);

        var enrichedResults = results.Select(r =>
        {
            if (ArticleStore.TryGetValue(r.KnowledgeArticleId, out var article))
            {
                r.Title = article.Title;
            }
            return r;
        });

        return enrichedResults;
    }

    public Task<KnowledgeArticle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ArticleStore.TryGetValue(id, out var article);
        return Task.FromResult(article);
    }

    public Task<IEnumerable<KnowledgeArticle>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ArticleStore.Values.AsEnumerable());
    }

    public async Task ProcessAndIndexDocumentAsync(
        SharePointDocument document, Stream content, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing and indexing document: {DocumentName}", document.Name);

        content.Position = 0;
        var text = await _documentProcessor.ExtractTextAsync(content, document.ContentType, cancellationToken);

        var articleId = ArticleStore.Values
            .FirstOrDefault(a => a.SourceDocumentId == document.Id)?.Id ?? Guid.NewGuid();

        var chunks = _documentProcessor.ChunkDocument(text, articleId).ToList();

        if (chunks.Count == 0)
        {
            _logger.LogWarning("No chunks generated from document: {DocumentName}", document.Name);
            return;
        }

        // Generate embeddings for all chunks
        var chunkTexts = chunks.Select(c => c.Content).ToList();
        var embeddings = await _embeddingService.GenerateEmbeddingsAsync(chunkTexts, cancellationToken);

        for (int i = 0; i < chunks.Count; i++)
        {
            chunks[i].Embedding = embeddings[i];
        }

        // Delete existing chunks and upsert new ones
        await _vectorStoreService.DeleteByArticleIdAsync(articleId, cancellationToken);
        await _vectorStoreService.UpsertChunksAsync(chunks, cancellationToken);

        _logger.LogInformation("Successfully indexed {ChunkCount} chunks for document: {DocumentName}",
            chunks.Count, document.Name);
    }

    private static string GenerateSummary(string text, int maxLength = 500)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        if (text.Length <= maxLength) return text;

        var truncated = text[..maxLength];
        var lastSentenceEnd = truncated.LastIndexOfAny(new[] { '.', '!', '?' });

        return lastSentenceEnd > 0 ? truncated[..(lastSentenceEnd + 1)] : truncated + "...";
    }
}
