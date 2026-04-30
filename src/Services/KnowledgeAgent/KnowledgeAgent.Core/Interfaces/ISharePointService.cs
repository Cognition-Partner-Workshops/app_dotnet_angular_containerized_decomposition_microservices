using KnowledgeAgent.Core.Models;

namespace KnowledgeAgent.Core.Interfaces;

public interface ISharePointService
{
    Task<IEnumerable<SharePointDocument>> GetDocumentsAsync(string siteId, string driveId, CancellationToken cancellationToken = default);
    Task<Stream> DownloadDocumentAsync(string driveId, string itemId, CancellationToken cancellationToken = default);
    Task<SharePointDocument?> GetDocumentMetadataAsync(string driveId, string itemId, CancellationToken cancellationToken = default);
    Task<IEnumerable<SharePointDocument>> GetModifiedDocumentsAsync(string siteId, string driveId, DateTime since, CancellationToken cancellationToken = default);
}
