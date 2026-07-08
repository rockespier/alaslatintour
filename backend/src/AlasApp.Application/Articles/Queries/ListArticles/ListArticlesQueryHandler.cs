using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Articles.Models;
using AlasApp.Application.Common;

namespace AlasApp.Application.Articles.Queries.ListArticles;

public sealed class ListArticlesQueryHandler(IWordPressService wordPressService)
    : IRequestHandler<ListArticlesQuery, PagedResult<ArticleSummaryDto>>
{
    public Task<PagedResult<ArticleSummaryDto>> Handle(ListArticlesQuery request, CancellationToken cancellationToken)
    {
        return wordPressService.ListArticlesAsync(request.Filter, cancellationToken);
    }
}
