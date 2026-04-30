using KnowledgeAgent.Core.Models;

namespace KnowledgeAgent.Core.Interfaces;

public interface IKnowledgeAgentOrchestrator
{
    Task SyncSharePointDocumentsAsync(CancellationToken cancellationToken = default);
    Task ProcessAlertAsync(AppInsightAlert alert, CancellationToken cancellationToken = default);
    Task RunMonitoringCycleAsync(CancellationToken cancellationToken = default);
}
