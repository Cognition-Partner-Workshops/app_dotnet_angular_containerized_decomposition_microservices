using KnowledgeAgent.Core.Interfaces;
using KnowledgeAgent.Core.Models;
using KnowledgeAgent.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace KnowledgeAgent.Infrastructure.Services;

public class QdrantVectorStoreService : IVectorStoreService
{
    private readonly QdrantClient _client;
    private readonly VectorStoreOptions _options;
    private readonly ILogger<QdrantVectorStoreService> _logger;

    public QdrantVectorStoreService(
        IOptions<VectorStoreOptions> options,
        ILogger<QdrantVectorStoreService> logger)
    {
        _options = options.Value;
        _logger = logger;

        _client = new QdrantClient(
            host: _options.Host,
            port: _options.Port,
            apiKey: string.IsNullOrEmpty(_options.ApiKey) ? null : _options.ApiKey);
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing vector store collection: {Collection}", _options.CollectionName);

        try
        {
            var collections = await _client.ListCollectionsAsync(cancellationToken);
            var collectionExists = collections.Any(c => c == _options.CollectionName);

            if (!collectionExists)
            {
                await _client.CreateCollectionAsync(
                    _options.CollectionName,
                    new VectorParams
                    {
                        Size = (ulong)_options.VectorSize,
                        Distance = Distance.Cosine
                    },
                    cancellationToken: cancellationToken);

                _logger.LogInformation("Created vector store collection: {Collection}", _options.CollectionName);
            }
            else
            {
                _logger.LogInformation("Vector store collection already exists: {Collection}", _options.CollectionName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing vector store");
            throw;
        }
    }

    public async Task UpsertChunksAsync(
        IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default)
    {
        var chunkList = chunks.ToList();
        _logger.LogInformation("Upserting {Count} chunks to vector store", chunkList.Count);

        try
        {
            var points = chunkList.Select(chunk => new PointStruct
            {
                Id = new PointId { Uuid = chunk.Id.ToString() },
                Vectors = chunk.Embedding,
                Payload =
                {
                    ["knowledge_article_id"] = chunk.KnowledgeArticleId.ToString(),
                    ["content"] = chunk.Content,
                    ["chunk_index"] = chunk.ChunkIndex.ToString(),
                    ["token_count"] = chunk.TokenCount.ToString()
                }
            }).ToList();

            await _client.UpsertAsync(
                _options.CollectionName,
                points,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Successfully upserted {Count} chunks", chunkList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting chunks to vector store");
            throw;
        }
    }

    public async Task<IEnumerable<SearchResult>> SearchAsync(
        float[] queryEmbedding, int topK = 5, float scoreThreshold = 0.7f,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Searching vector store with top-K={TopK}, threshold={Threshold}", topK, scoreThreshold);

        try
        {
            var searchResults = await _client.SearchAsync(
                _options.CollectionName,
                queryEmbedding,
                limit: (ulong)topK,
                scoreThreshold: scoreThreshold,
                cancellationToken: cancellationToken);

            return searchResults.Select(r => new SearchResult
            {
                KnowledgeArticleId = Guid.Parse(r.Payload["knowledge_article_id"].StringValue),
                Content = r.Payload["content"].StringValue,
                Score = r.Score,
                Metadata = new Dictionary<string, string>
                {
                    ["chunk_index"] = r.Payload["chunk_index"].StringValue,
                    ["token_count"] = r.Payload["token_count"].StringValue
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching vector store");
            throw;
        }
    }

    public async Task DeleteByArticleIdAsync(Guid knowledgeArticleId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting chunks for article {ArticleId}", knowledgeArticleId);

        try
        {
            await _client.DeleteAsync(
                _options.CollectionName,
                new Filter
                {
                    Must =
                    {
                        new Condition
                        {
                            Field = new FieldCondition
                            {
                                Key = "knowledge_article_id",
                                Match = new Match { Text = knowledgeArticleId.ToString() }
                            }
                        }
                    }
                },
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting chunks from vector store");
            throw;
        }
    }
}
