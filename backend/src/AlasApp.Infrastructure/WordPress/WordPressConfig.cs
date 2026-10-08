namespace AlasApp.Infrastructure.WordPress;

public sealed class WordPressConfig
{
    public const string SectionName = "WordPressConfig";

    public string BaseUrl { get; init; } = string.Empty;

    public string PostsBaseUrl { get; init; } = string.Empty;

    public string GalleriesBaseUrl { get; init; } = string.Empty;

    public string MediaBaseUrl { get; init; } = string.Empty;

    /// <summary>`wp/v2/pages`; falls back to <see cref="BaseUrl"/> when empty.</summary>
    public string PagesBaseUrl { get; init; } = string.Empty;

    public string Username { get; init; } = string.Empty;

    public string AppPassword { get; init; } = string.Empty;
}
