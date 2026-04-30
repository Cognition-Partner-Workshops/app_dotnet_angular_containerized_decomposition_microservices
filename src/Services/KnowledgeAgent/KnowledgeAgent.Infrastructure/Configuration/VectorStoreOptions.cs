namespace KnowledgeAgent.Infrastructure.Configuration;

public class VectorStoreOptions
{
    public const string SectionName = "VectorStore";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 6334;
    public string CollectionName { get; set; } = "knowledge_articles";
    public int VectorSize { get; set; } = 1536;
    public string ApiKey { get; set; } = string.Empty;
}
