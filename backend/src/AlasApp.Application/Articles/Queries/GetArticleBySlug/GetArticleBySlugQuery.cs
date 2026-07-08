using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Articles.Models;

namespace AlasApp.Application.Articles.Queries.GetArticleBySlug;

public sealed record GetArticleBySlugQuery(string Slug) : IRequest<ArticleDetailDto>;
