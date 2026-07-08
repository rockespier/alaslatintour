using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Articles.Models;
using AlasApp.Domain.Enums;

namespace AlasApp.Application.Articles.Commands.UpdateArticle;

public sealed record UpdateArticleCommand(
    string Slug,
    string Titulo,
    string Resumen,
    string ContentHtml,
    ArticleCategory Categoria,
    string Autor,
    string AutorTitulo,
    string ImagenUrl,
    IReadOnlyCollection<string> Tags,
    bool Featured,
    bool ShowRankingWidget,
    string? RelatedEventId) : IRequest<ArticleDetailDto>;
