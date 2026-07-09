using System.Text.Json.Serialization;

namespace AlasApp.Infrastructure.WordPress;

internal sealed record WordPressGalleryPhotoDto(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("width")] int Width,
    [property: JsonPropertyName("height")] int Height);

internal sealed record WordPressGalleryDayDto(
    [property: JsonPropertyName("day_name")] string? DayName,
    [property: JsonPropertyName("photos")] IReadOnlyList<WordPressGalleryPhotoDto>? Photos);

internal sealed record WordPressGalleryAcfDto(
    [property: JsonPropertyName("gallery_days")] IReadOnlyList<WordPressGalleryDayDto>? GalleryDays,
    [property: JsonPropertyName("press_download_link")] string? PressDownloadLink,
    [property: JsonPropertyName("event_date")] string? EventDate);

internal sealed record WordPressGalleryPostDto(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("slug")] string Slug,
    [property: JsonPropertyName("title")] WordPressRenderedDto Title,
    [property: JsonPropertyName("acf")] WordPressGalleryAcfDto? Acf);
