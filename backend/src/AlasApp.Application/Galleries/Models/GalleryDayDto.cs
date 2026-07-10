namespace AlasApp.Application.Galleries.Models;

public sealed record GalleryDayDto(
    string DayName,
    IReadOnlyCollection<GalleryAssetDto> Assets);
