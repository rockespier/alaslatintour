using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace AlasApp.Api.Models;

[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public sealed record GalleryAssetResponse(string Id, string Type, string Url, int Width, int Height);

[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public sealed record GalleryDayResponse(string DayName, IReadOnlyCollection<GalleryAssetResponse> Assets);

[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public sealed record GallerySummaryResponse(
    string Id,
    string Slug,
    string Title,
    DateTimeOffset? EventDate,
    string? CoverImageUrl,
    int PhotoCount);

[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public sealed record GalleryDetailResponse(
    string Id,
    string Slug,
    string Title,
    DateTimeOffset? EventDate,
    string? PressDownloadLink,
    string? CoverImageUrl,
    int PhotoCount,
    IReadOnlyCollection<GalleryDayResponse> GalleryDays);

[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public sealed record GalleryListResponse(IReadOnlyCollection<GallerySummaryResponse> Data);
