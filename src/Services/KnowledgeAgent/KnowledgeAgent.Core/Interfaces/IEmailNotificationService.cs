using KnowledgeAgent.Core.Models;

namespace KnowledgeAgent.Core.Interfaces;

public interface IEmailNotificationService
{
    Task SendAlertWithKnowledgeAsync(AppInsightAlert alert, IEnumerable<SearchResult> relevantArticles, IEnumerable<string> recipients, CancellationToken cancellationToken = default);
    Task SendDigestEmailAsync(IEnumerable<AppInsightAlert> alerts, IEnumerable<string> recipients, CancellationToken cancellationToken = default);
}
