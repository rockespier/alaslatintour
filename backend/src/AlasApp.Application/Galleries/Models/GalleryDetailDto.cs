namespace AlasApp.Application.Galleries.Models;

public sealed record GalleryDetailDto(
    string Id,
    string Slug,
    string Title,
    DateTimeOffset? EventDate,
    string? PressDownloadLink,
    string? CoverImageUrl,
    int PhotoCount,
    IReadOnlyCollection<GalleryDayDto> GalleryDays);
