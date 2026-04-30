namespace KnowledgeAgent.Core.Models;

public class AppInsightAlert
{
    public string AlertId { get; set; } = string.Empty;
    public string AlertName { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AffectedResource { get; set; } = string.Empty;
    public DateTime FiredAt { get; set; }
    public string ExceptionType { get; set; } = string.Empty;
    public string ExceptionMessage { get; set; } = string.Empty;
    public string StackTrace { get; set; } = string.Empty;
    public Dictionary<string, string> CustomProperties { get; set; } = new();
}
