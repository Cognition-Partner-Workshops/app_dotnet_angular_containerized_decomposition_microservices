using KnowledgeAgent.Core.Models;

namespace KnowledgeAgent.Core.Interfaces;

public interface IAppInsightsService
{
    Task<IEnumerable<AppInsightAlert>> GetRecentAlertsAsync(TimeSpan lookbackPeriod, CancellationToken cancellationToken = default);
    Task<IEnumerable<AppInsightAlert>> GetActiveExceptionsAsync(CancellationToken cancellationToken = default);
    void TrackEvent(string eventName, Dictionary<string, string>? properties = null);
    void TrackException(Exception exception, Dictionary<string, string>? properties = null);
}
