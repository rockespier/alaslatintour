using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace AlasApp.Api.Models;

[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public sealed record GalleryPhotoResponse(string Id, string Url, int Width, int Height);

[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public sealed record GalleryResponse(
    string Id,
    string Slug,
    string Title,
    DateTimeOffset? EventDate,
    string? PressDownloadLink,
    IReadOnlyCollection<GalleryPhotoResponse> Photos);

[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public sealed record GalleryListResponse(IReadOnlyCollection<GalleryResponse> Data);
