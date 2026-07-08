using AlasApp.Domain.Enums;

namespace AlasApp.Application.Articles.Models;

public sealed record ArticleSummaryDto(
    string Id,
    string Titulo,
    string Resumen,
    ArticleCategory Categoria,
    string Autor,
    string AutorTitulo,
    string ImagenUrl,
    IReadOnlyCollection<string> Tags,
    bool Featured,
    string? RelatedEventId,
    string Slug,
    DateTimeOffset FechaPublicacion,
    int TiempoLecturaMin);
