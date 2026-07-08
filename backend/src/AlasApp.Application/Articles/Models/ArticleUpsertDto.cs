using AlasApp.Domain.Enums;

namespace AlasApp.Application.Articles.Models;

public sealed record ArticleUpsertDto(
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
    string? RelatedEventId);
