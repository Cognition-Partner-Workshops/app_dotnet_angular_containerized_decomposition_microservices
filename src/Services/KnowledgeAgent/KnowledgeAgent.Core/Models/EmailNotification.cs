namespace KnowledgeAgent.Core.Models;

public class EmailNotification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public List<string> Recipients { get; set; } = new();
    public string AlertId { get; set; } = string.Empty;
    public List<Guid> ReferencedArticleIds { get; set; } = new();
    public DateTime SentAt { get; set; }
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
}

public enum NotificationStatus
{
    Pending,
    Sent,
    Failed
}
