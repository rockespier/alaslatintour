namespace AlasApp.Application.Galleries.Models;

public sealed record GallerySummaryDto(
    string Id,
    string Slug,
    string Title,
    DateTimeOffset? EventDate,
    string? CoverImageUrl,
    int PhotoCount,
    /// <summary>Publication date set in WordPress.</summary>
    DateTimeOffset? PublishedAt = null);
