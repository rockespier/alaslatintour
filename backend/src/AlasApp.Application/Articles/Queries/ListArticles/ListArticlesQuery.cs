using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Articles.Models;
using AlasApp.Application.Common;

namespace AlasApp.Application.Articles.Queries.ListArticles;

public sealed record ListArticlesQuery(ArticleListFilter Filter) : IRequest<PagedResult<ArticleSummaryDto>>;
