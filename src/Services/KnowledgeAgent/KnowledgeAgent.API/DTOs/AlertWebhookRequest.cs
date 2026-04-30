namespace KnowledgeAgent.API.DTOs;

public class AlertWebhookRequest
{
    public string AlertId { get; set; } = string.Empty;
    public string AlertName { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AffectedResource { get; set; } = string.Empty;
    public string ExceptionType { get; set; } = string.Empty;
    public string ExceptionMessage { get; set; } = string.Empty;
    public string StackTrace { get; set; } = string.Empty;
    public Dictionary<string, string>? CustomProperties { get; set; }
}
