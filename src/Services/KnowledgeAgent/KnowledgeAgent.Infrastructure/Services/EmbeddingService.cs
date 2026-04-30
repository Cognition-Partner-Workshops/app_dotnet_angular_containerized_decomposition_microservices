using System.ClientModel;
using Azure;
using Azure.AI.OpenAI;
using KnowledgeAgent.Core.Interfaces;
using KnowledgeAgent.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Embeddings;

namespace KnowledgeAgent.Infrastructure.Services;

public class EmbeddingService : IEmbeddingService
{
    private readonly EmbeddingClient _embeddingClient;
    private readonly OpenAIOptions _options;
    private readonly ILogger<EmbeddingService> _logger;

    public EmbeddingService(
        IOptions<OpenAIOptions> options,
        ILogger<EmbeddingService> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (_options.UseAzureOpenAI)
        {
            var azureClient = new AzureOpenAIClient(
                new Uri(_options.Endpoint),
                new ApiKeyCredential(_options.ApiKey));
            _embeddingClient = azureClient.GetEmbeddingClient(_options.EmbeddingModel);
        }
        else
        {
            var openAiClient = new OpenAI.OpenAIClient(new ApiKeyCredential(_options.ApiKey));
            _embeddingClient = openAiClient.GetEmbeddingClient(_options.EmbeddingModel);
        }
    }

    public async Task<float[]> GenerateEmbeddingAsync(
        string text, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Generating embedding for text of length {Length}", text.Length);

        try
        {
            var response = await _embeddingClient.GenerateEmbeddingAsync(text);
            var embedding = response.Value;
            return embedding.ToFloats().ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embedding");
            throw;
        }
    }

    public async Task<IList<float[]>> GenerateEmbeddingsAsync(
        IEnumerable<string> texts, CancellationToken cancellationToken = default)
    {
        var textList = texts.ToList();
        _logger.LogInformation("Generating embeddings for {Count} texts", textList.Count);

        try
        {
            var response = await _embeddingClient.GenerateEmbeddingsAsync(textList);
            return response.Value
                .OrderBy(e => e.Index)
                .Select(e => e.ToFloats().ToArray())
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating batch embeddings");
            throw;
        }
    }
}
