namespace AlasApp.Application.Galleries.Models;

public sealed record GalleryDto(
    string Id,
    string Slug,
    string Title,
    DateTimeOffset? EventDate,
    string? PressDownloadLink,
    IReadOnlyCollection<GalleryPhotoDto> Photos);
