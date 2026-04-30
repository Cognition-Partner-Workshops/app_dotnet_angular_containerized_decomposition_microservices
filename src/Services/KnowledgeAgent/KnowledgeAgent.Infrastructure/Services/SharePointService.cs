using Azure.Identity;
using KnowledgeAgent.Core.Interfaces;
using KnowledgeAgent.Core.Models;
using KnowledgeAgent.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace KnowledgeAgent.Infrastructure.Services;

public class SharePointService : ISharePointService
{
    private readonly GraphServiceClient _graphClient;
    private readonly SharePointOptions _options;
    private readonly ILogger<SharePointService> _logger;

    public SharePointService(
        IOptions<SharePointOptions> options,
        ILogger<SharePointService> logger)
    {
        _options = options.Value;
        _logger = logger;

        var credential = new ClientSecretCredential(
            _options.TenantId,
            _options.ClientId,
            _options.ClientSecret);

        _graphClient = new GraphServiceClient(credential, new[] { "https://graph.microsoft.com/.default" });
    }

    public async Task<IEnumerable<SharePointDocument>> GetDocumentsAsync(
        string siteId, string driveId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching documents from SharePoint site {SiteId}, drive {DriveId}", siteId, driveId);

        var documents = new List<SharePointDocument>();

        try
        {
            var driveItems = await _graphClient.Drives[driveId].Items["root"].Children
                .GetAsync(cancellationToken: cancellationToken);

            if (driveItems?.Value == null) return documents;

            foreach (var item in driveItems.Value)
            {
                if (item.File != null)
                {
                    documents.Add(MapToSharePointDocument(item, siteId, driveId));
                }
                else if (item.Folder != null)
                {
                    var folderDocs = await GetFolderDocumentsRecursiveAsync(driveId, item.Id!, siteId, cancellationToken);
                    documents.AddRange(folderDocs);
                }
            }

            _logger.LogInformation("Found {Count} documents in SharePoint", documents.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching documents from SharePoint");
            throw;
        }

        return documents;
    }

    public async Task<Stream> DownloadDocumentAsync(
        string driveId, string itemId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Downloading document {ItemId} from drive {DriveId}", itemId, driveId);

        var stream = await _graphClient.Drives[driveId].Items[itemId].Content
            .GetAsync(cancellationToken: cancellationToken);

        if (stream == null)
            throw new InvalidOperationException($"Failed to download document {itemId}");

        return stream;
    }

    public async Task<SharePointDocument?> GetDocumentMetadataAsync(
        string driveId, string itemId, CancellationToken cancellationToken = default)
    {
        try
        {
            var item = await _graphClient.Drives[driveId].Items[itemId]
                .GetAsync(cancellationToken: cancellationToken);

            if (item == null) return null;

            return MapToSharePointDocument(item, string.Empty, driveId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching document metadata for {ItemId}", itemId);
            return null;
        }
    }

    public async Task<IEnumerable<SharePointDocument>> GetModifiedDocumentsAsync(
        string siteId, string driveId, DateTime since, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching documents modified since {Since} from SharePoint", since);

        var allDocuments = await GetDocumentsAsync(siteId, driveId, cancellationToken);
        return allDocuments.Where(d => d.LastModified > since);
    }

    private async Task<IEnumerable<SharePointDocument>> GetFolderDocumentsRecursiveAsync(
        string driveId, string folderId, string siteId, CancellationToken cancellationToken)
    {
        var documents = new List<SharePointDocument>();

        var children = await _graphClient.Drives[driveId].Items[folderId].Children
            .GetAsync(cancellationToken: cancellationToken);

        if (children?.Value == null) return documents;

        foreach (var item in children.Value)
        {
            if (item.File != null)
            {
                documents.Add(MapToSharePointDocument(item, siteId, driveId));
            }
            else if (item.Folder != null)
            {
                var subDocs = await GetFolderDocumentsRecursiveAsync(driveId, item.Id!, siteId, cancellationToken);
                documents.AddRange(subDocs);
            }
        }

        return documents;
    }

    private static SharePointDocument MapToSharePointDocument(DriveItem item, string siteId, string driveId)
    {
        return new SharePointDocument
        {
            Id = item.Id ?? string.Empty,
            Name = item.Name ?? string.Empty,
            ContentType = item.File?.MimeType ?? string.Empty,
            Size = item.Size ?? 0,
            WebUrl = item.WebUrl ?? string.Empty,
            DriveId = driveId,
            SiteId = siteId,
            LastModified = item.LastModifiedDateTime?.UtcDateTime ?? DateTime.MinValue,
            LastModifiedBy = item.LastModifiedBy?.User?.DisplayName ?? string.Empty
        };
    }
}
