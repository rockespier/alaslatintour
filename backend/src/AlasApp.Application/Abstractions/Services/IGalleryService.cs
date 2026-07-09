using AlasApp.Application.Galleries.Models;

namespace AlasApp.Application.Abstractions.Services;

public interface IGalleryService
{
    Task<IReadOnlyCollection<GalleryDto>> ListAsync(CancellationToken cancellationToken);
}
