using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Articles.Models;
using AlasApp.Application.Common;

namespace AlasApp.Application.Articles.Queries.GetArticleBySlug;

public sealed class GetArticleBySlugQueryHandler(IWordPressService wordPressService)
    : IRequestHandler<GetArticleBySlugQuery, ArticleDetailDto>
{
    public async Task<ArticleDetailDto> Handle(GetArticleBySlugQuery request, CancellationToken cancellationToken)
    {
        var article = await wordPressService.GetBySlugAsync(request.Slug, cancellationToken);
        return article ?? throw new NotFoundException("No se encontró el artículo solicitado.");
    }
}
