using System.Text.Json.Serialization;

namespace AlasApp.Infrastructure.WordPress;

internal sealed record WordPressRenderedDto(
    [property: JsonPropertyName("rendered")] string Rendered);

internal sealed record WordPressAuthorDto(
    [property: JsonPropertyName("name")] string Name);

internal sealed record WordPressMediaSizeDto(
    [property: JsonPropertyName("source_url")] string? SourceUrl);

internal sealed record WordPressMediaDetailsDto(
    [property: JsonPropertyName("full")] WordPressMediaSizeDto? Full,
    [property: JsonPropertyName("large")] WordPressMediaSizeDto? Large,
    [property: JsonPropertyName("medium_large")] WordPressMediaSizeDto? MediumLarge,
    [property: JsonPropertyName("medium")] WordPressMediaSizeDto? Medium);

internal sealed record WordPressFeaturedMediaDto(
    [property: JsonPropertyName("source_url")] string? SourceUrl,
    [property: JsonPropertyName("media_details")] WordPressMediaDetailsDto? MediaDetails);

internal sealed record WordPressTermDto(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("taxonomy")] string? Taxonomy);

internal sealed record WordPressEmbeddedDto(
    [property: JsonPropertyName("author")] IReadOnlyList<WordPressAuthorDto>? Author,
    [property: JsonPropertyName("wp:featuredmedia")] IReadOnlyList<WordPressFeaturedMediaDto>? FeaturedMedia,
    [property: JsonPropertyName("wp:term")] IReadOnlyList<IReadOnlyList<WordPressTermDto>>? Terms);

internal sealed record WordPressMetaDto(
    [property: JsonPropertyName("author_role")] string? AuthorRole,
    [property: JsonPropertyName("read_time_minutes")] int? ReadTimeMinutes,
    [property: JsonPropertyName("show_ranking")] bool ShowRanking,
    [property: JsonPropertyName("featured")] bool? Featured,
    [property: JsonPropertyName("article_category")] string? ArticleCategory,
    [property: JsonPropertyName("related_event_id")] string? RelatedEventId,
    [property: JsonPropertyName("image_url")] string? ImageUrl,
    [property: JsonPropertyName("tags_csv")] string? TagsCsv);

internal sealed record WordPressPostDto(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("date")] DateTimeOffset Date,
    [property: JsonPropertyName("slug")] string Slug,
    [property: JsonPropertyName("title")] WordPressRenderedDto Title,
    [property: JsonPropertyName("excerpt")] WordPressRenderedDto? Excerpt,
    [property: JsonPropertyName("content")] WordPressRenderedDto? Content,
    [property: JsonPropertyName("featured_media")] int FeaturedMedia,
    [property: JsonPropertyName("sticky")] bool Sticky,
    [property: JsonPropertyName("tags")] IReadOnlyList<int>? Tags,
    [property: JsonPropertyName("meta")] WordPressMetaDto? Meta,
    [property: JsonPropertyName("_embedded")] WordPressEmbeddedDto? Embedded);

internal sealed record WordPressCreateUpdateRequest(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("excerpt")] string Excerpt,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("slug")] string Slug,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("sticky")] bool Sticky,
    [property: JsonPropertyName("meta")] WordPressMutableMetaDto Meta);

internal sealed record WordPressMutableMetaDto(
    [property: JsonPropertyName("author_role")] string AuthorRole,
    [property: JsonPropertyName("read_time_minutes")] int ReadTimeMinutes,
    [property: JsonPropertyName("show_ranking")] bool ShowRanking,
    [property: JsonPropertyName("featured")] bool Featured,
    [property: JsonPropertyName("article_category")] string ArticleCategory,
    [property: JsonPropertyName("related_event_id")] string? RelatedEventId,
    [property: JsonPropertyName("image_url")] string ImageUrl,
    [property: JsonPropertyName("tags_csv")] string TagsCsv);
