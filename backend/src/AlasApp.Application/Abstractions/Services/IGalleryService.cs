using AlasApp.Application.Galleries.Models;

namespace AlasApp.Application.Abstractions.Services;

public interface IGalleryService
{
    Task<IReadOnlyCollection<GallerySummaryDto>> ListAsync(string? lang, CancellationToken cancellationToken);

    Task<GalleryDetailDto?> GetBySlugAsync(string slug, string? lang, CancellationToken cancellationToken);
}
