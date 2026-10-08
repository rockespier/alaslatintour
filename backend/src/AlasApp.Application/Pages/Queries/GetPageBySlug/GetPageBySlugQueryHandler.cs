using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Common;
using AlasApp.Application.Pages.Models;

namespace AlasApp.Application.Pages.Queries.GetPageBySlug;

public sealed class GetPageBySlugQueryHandler(IPageService pageService)
    : IRequestHandler<GetPageBySlugQuery, PageContentDto>
{
    public async Task<PageContentDto> Handle(GetPageBySlugQuery request, CancellationToken cancellationToken)
    {
        return await pageService.GetBySlugAsync(request.Slug, request.Lang, cancellationToken)
            ?? throw new NotFoundException("Pagina no encontrada.");
    }
}
