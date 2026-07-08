using AlasApp.Domain.Enums;

namespace AlasApp.Application.Articles.Models;

public sealed record ArticleListFilter(
    int Page,
    int Limit,
    ArticleCategory? Category,
    bool? Featured,
    string? Search);
