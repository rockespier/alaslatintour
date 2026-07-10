namespace AlasApp.Application.Galleries.Models;

public sealed record GallerySummaryDto(
    string Id,
    string Slug,
    string Title,
    DateTimeOffset? EventDate,
    string? CoverImageUrl,
    int PhotoCount);
