namespace AIMonitoringAgent.Models;

public class AIModelMetrics
{
    public string ModelName { get; set; } = string.Empty;
    public string ModelVersion { get; set; } = string.Empty;
    public double Latency { get; set; }
    public double TokensUsed { get; set; }
    public double PromptTokens { get; set; }
    public double CompletionTokens { get; set; }
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, string> CustomProperties { get; set; } = new();
}
