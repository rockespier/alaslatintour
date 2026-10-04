using AlasApp.Domain.Enums;

namespace AlasApp.Application.Articles.Models;

public sealed record ArticleDetailDto(
    string Id,
    string Titulo,
    string Resumen,
    string ContentHtml,
    ArticleCategory Categoria,
    ArticleAuthorDto Author,
    string ImagenUrl,
    IReadOnlyCollection<string> Tags,
    bool Featured,
    bool ShowRankingWidget,
    string? RelatedEventId,
    string Slug,
    DateTimeOffset FechaPublicacion,
    int TiempoLecturaMin,
    /// <summary>Slug of this article in each language (Polylang), including its own.</summary>
    IReadOnlyDictionary<string, string>? Translations = null);
