using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Common;
using AlasApp.Application.Galleries.Models;
using AlasApp.Domain.Exceptions;

namespace AlasApp.Application.Galleries.Queries.GetGalleryBySlug;

public sealed class GetGalleryBySlugQueryHandler(IGalleryService galleryService)
    : IRequestHandler<GetGalleryBySlugQuery, GalleryDetailDto>
{
    public async Task<GalleryDetailDto> Handle(GetGalleryBySlugQuery request, CancellationToken cancellationToken)
    {
        return await galleryService.GetBySlugAsync(request.Slug, cancellationToken)
            ?? throw new NotFoundException("Galeria no encontrada.");
    }
}
