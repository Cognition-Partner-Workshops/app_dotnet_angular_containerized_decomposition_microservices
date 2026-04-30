namespace KnowledgeAgent.Infrastructure.Configuration;

public class OpenAIOptions
{
    public const string SectionName = "OpenAI";

    public string ApiKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string EmbeddingModel { get; set; } = "text-embedding-ada-002";
    public string CompletionModel { get; set; } = "gpt-4";
    public bool UseAzureOpenAI { get; set; } = true;
    public string AzureDeploymentName { get; set; } = string.Empty;
}
