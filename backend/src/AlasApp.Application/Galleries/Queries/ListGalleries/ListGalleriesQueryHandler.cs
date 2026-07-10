using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Galleries.Models;

namespace AlasApp.Application.Galleries.Queries.ListGalleries;

public sealed class ListGalleriesQueryHandler(IGalleryService galleryService)
    : IRequestHandler<ListGalleriesQuery, IReadOnlyCollection<GallerySummaryDto>>
{
    public Task<IReadOnlyCollection<GallerySummaryDto>> Handle(ListGalleriesQuery request, CancellationToken cancellationToken)
        => galleryService.ListAsync(cancellationToken);
}
