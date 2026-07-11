using System.Text.Json.Serialization;

namespace AlasApp.Infrastructure.WordPress;

internal sealed record WordPressMediaUploadResponseDto(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("source_url")] string? SourceUrl,
    [property: JsonPropertyName("mime_type")] string? MimeType);
