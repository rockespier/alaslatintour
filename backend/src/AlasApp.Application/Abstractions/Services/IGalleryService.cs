using AlasApp.Application.Galleries.Models;

namespace AlasApp.Application.Abstractions.Services;

public interface IGalleryService
{
    Task<IReadOnlyCollection<GallerySummaryDto>> ListAsync(CancellationToken cancellationToken);

    Task<GalleryDetailDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken);
}
