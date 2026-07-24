using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.IdentityDocuments;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

namespace AlasApp.Infrastructure.IdentityDocuments;

public sealed class AzureBlobIdentityDocumentStorage(IOptions<AzureBlobIdentityDocumentOptions> options) : IIdentityDocumentStorage
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "application/pdf"
    };

    public async Task<string> UploadAsync(Guid competitorId, IdentityDocumentUpload document, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Value.ConnectionString))
        {
            throw new InvalidOperationException("Azure Blob Storage no está configurado para documentos de identidad.");
        }

        if (!AllowedContentTypes.Contains(document.ContentType))
        {
            throw new InvalidOperationException("El documento de identidad debe ser JPG, PNG, WebP o PDF.");
        }

        var container = new BlobContainerClient(options.Value.ConnectionString, options.Value.ContainerName);
        await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);

        var extension = Path.GetExtension(document.FileName);
        var blobName = $"competitors/{competitorId:N}/identity/{Guid.NewGuid():N}{extension}";
        var blob = container.GetBlobClient(blobName);

        await blob.UploadAsync(
            document.Content,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = document.ContentType },
                Metadata = new Dictionary<string, string>
                {
                    ["competitorId"] = competitorId.ToString("D"),
                    ["originalFileName"] = document.FileName
                }
            },
            cancellationToken);

        return blobName;
    }

    public async Task<IdentityDocumentDownload?> DownloadAsync(string blobName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Value.ConnectionString))
        {
            throw new InvalidOperationException("Azure Blob Storage no está configurado para documentos de identidad.");
        }

        var container = new BlobContainerClient(options.Value.ConnectionString, options.Value.ContainerName);
        var blob = container.GetBlobClient(blobName);

        if (!await blob.ExistsAsync(cancellationToken))
        {
            return null;
        }

        var download = await blob.DownloadStreamingAsync(cancellationToken: cancellationToken);
        var fileName = download.Value.Details.Metadata.TryGetValue("originalFileName", out var originalFileName)
            ? originalFileName
            : blobName.Split('/')[^1];

        return new IdentityDocumentDownload(download.Value.Content, download.Value.Details.ContentType, fileName);
    }
}
