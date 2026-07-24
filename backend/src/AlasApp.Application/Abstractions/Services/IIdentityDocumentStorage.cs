using AlasApp.Application.IdentityDocuments;

namespace AlasApp.Application.Abstractions.Services;

public interface IIdentityDocumentStorage
{
    Task<string> UploadAsync(Guid competitorId, IdentityDocumentUpload document, CancellationToken cancellationToken);

    Task<IdentityDocumentDownload?> DownloadAsync(string blobName, CancellationToken cancellationToken);
}
