using KnowledgeAgent.Core.Interfaces;
using KnowledgeAgent.Core.Models;
using KnowledgeAgent.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KnowledgeAgent.Infrastructure.Services;

public class KnowledgeAgentOrchestrator : IKnowledgeAgentOrchestrator
{
    private readonly ISharePointService _sharePointService;
    private readonly IKnowledgeArticleService _knowledgeArticleService;
    private readonly IAppInsightsService _appInsightsService;
    private readonly IEmailNotificationService _emailNotificationService;
    private readonly SharePointOptions _sharePointOptions;
    private readonly EmailOptions _emailOptions;
    private readonly ILogger<KnowledgeAgentOrchestrator> _logger;

    private DateTime _lastSyncTime = DateTime.MinValue;

    public KnowledgeAgentOrchestrator(
        ISharePointService sharePointService,
        IKnowledgeArticleService knowledgeArticleService,
        IAppInsightsService appInsightsService,
        IEmailNotificationService emailNotificationService,
        IOptions<SharePointOptions> sharePointOptions,
        IOptions<EmailOptions> emailOptions,
        ILogger<KnowledgeAgentOrchestrator> logger)
    {
        _sharePointService = sharePointService;
        _knowledgeArticleService = knowledgeArticleService;
        _appInsightsService = appInsightsService;
        _emailNotificationService = emailNotificationService;
        _sharePointOptions = sharePointOptions.Value;
        _emailOptions = emailOptions.Value;
        _logger = logger;
    }

    public async Task SyncSharePointDocumentsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting SharePoint document sync");

        try
        {
            IEnumerable<SharePointDocument> documents;

            if (_lastSyncTime == DateTime.MinValue)
            {
                documents = await _sharePointService.GetDocumentsAsync(
                    _sharePointOptions.SiteId,
                    _sharePointOptions.DriveId,
                    cancellationToken);
            }
            else
            {
                documents = await _sharePointService.GetModifiedDocumentsAsync(
                    _sharePointOptions.SiteId,
                    _sharePointOptions.DriveId,
                    _lastSyncTime,
                    cancellationToken);
            }

            var documentList = documents.ToList();
            _logger.LogInformation("Found {Count} documents to process", documentList.Count);

            foreach (var document in documentList)
            {
                try
                {
                    await using var contentStream = await _sharePointService.DownloadDocumentAsync(
                        document.DriveId, document.Id, cancellationToken)
                        as Stream ?? Stream.Null;

                    using var memoryStream = new MemoryStream();
                    await contentStream.CopyToAsync(memoryStream, cancellationToken);
                    memoryStream.Position = 0;

                    await _knowledgeArticleService.CreateFromDocumentAsync(document, memoryStream, cancellationToken);

                    _logger.LogInformation("Successfully processed document: {DocumentName}", document.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing document: {DocumentName}", document.Name);
                }
            }

            _lastSyncTime = DateTime.UtcNow;
            _logger.LogInformation("SharePoint document sync completed. Processed {Count} documents", documentList.Count);

            _appInsightsService.TrackEvent("SharePointSyncCompleted", new Dictionary<string, string>
            {
                ["DocumentCount"] = documentList.Count.ToString(),
                ["SyncTime"] = _lastSyncTime.ToString("O")
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during SharePoint document sync");
            _appInsightsService.TrackException(ex, new Dictionary<string, string>
            {
                ["Operation"] = "SharePointSync"
            });
            throw;
        }
    }

    public async Task ProcessAlertAsync(AppInsightAlert alert, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing alert: {AlertId} - {AlertName}", alert.AlertId, alert.AlertName);

        try
        {
            // Build a search query from the alert details
            var searchQuery = BuildSearchQuery(alert);

            // Search knowledge base for relevant articles
            var relevantArticles = await _knowledgeArticleService.SearchKnowledgeAsync(
                searchQuery, topK: 5, cancellationToken: cancellationToken);

            var articleList = relevantArticles.ToList();
            _logger.LogInformation("Found {Count} relevant knowledge articles for alert", articleList.Count);

            // Send email notification with relevant knowledge articles
            await _emailNotificationService.SendAlertWithKnowledgeAsync(
                alert, articleList, _emailOptions.DefaultRecipients, cancellationToken);

            _appInsightsService.TrackEvent("AlertProcessed", new Dictionary<string, string>
            {
                ["AlertId"] = alert.AlertId,
                ["RelevantArticlesCount"] = articleList.Count.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing alert: {AlertId}", alert.AlertId);
            _appInsightsService.TrackException(ex, new Dictionary<string, string>
            {
                ["AlertId"] = alert.AlertId,
                ["Operation"] = "ProcessAlert"
            });
            throw;
        }
    }

    public async Task RunMonitoringCycleAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Running monitoring cycle");

        try
        {
            var alerts = await _appInsightsService.GetActiveExceptionsAsync(cancellationToken);
            var alertList = alerts.ToList();

            if (alertList.Count == 0)
            {
                _logger.LogDebug("No active alerts found in monitoring cycle");
                return;
            }

            _logger.LogInformation("Found {Count} active alerts to process", alertList.Count);

            foreach (var alert in alertList)
            {
                await ProcessAlertAsync(alert, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during monitoring cycle");
            _appInsightsService.TrackException(ex, new Dictionary<string, string>
            {
                ["Operation"] = "MonitoringCycle"
            });
        }
    }

    private static string BuildSearchQuery(AppInsightAlert alert)
    {
        var queryParts = new List<string>();

        if (!string.IsNullOrEmpty(alert.ExceptionType))
            queryParts.Add(alert.ExceptionType);
        if (!string.IsNullOrEmpty(alert.ExceptionMessage))
            queryParts.Add(alert.ExceptionMessage);
        if (!string.IsNullOrEmpty(alert.Description))
            queryParts.Add(alert.Description);
        if (!string.IsNullOrEmpty(alert.AffectedResource))
            queryParts.Add(alert.AffectedResource);

        return string.Join(" ", queryParts);
    }
}
