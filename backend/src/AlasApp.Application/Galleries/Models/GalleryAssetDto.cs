namespace AlasApp.Application.Galleries.Models;

public sealed record GalleryAssetDto(
    string Id,
    GalleryAssetType Type,
    string Url,
    int Width,
    int Height);
