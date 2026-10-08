using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Pages.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace AlasApp.Infrastructure.WordPress;

/// <summary>
/// Static pages edited in WordPress (`wp/v2/pages`). The Spanish slug identifies the page; the other
/// languages are found through the Polylang `translations` field. Content is reduced to plain-text
/// paragraphs split by H2 headings, so Angular never renders WordPress HTML.
/// </summary>
public sealed partial class WordPressPageService(
    HttpClient httpClient,
    WordPressPageCache cache,
    ILogger<WordPressPageService> logger) : IPageService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const string DefaultLang = "es";

    public async Task<PageContentDto?> GetBySlugAsync(string slug, string? lang, CancellationToken cancellationToken)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        var targetLang = string.IsNullOrWhiteSpace(lang) ? DefaultLang : lang;

        try
        {
            return await cache.GetOrAddAsync($"{normalizedSlug}|{targetLang}", ct => LoadAsync(normalizedSlug, targetLang, ct), cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException)
        {
            logger.LogWarning(ex, "Could not read WordPress page {Slug} ({Lang})", normalizedSlug, targetLang);
            return null;
        }
    }

    private async Task<PageContentDto?> LoadAsync(string slug, string lang, CancellationToken cancellationToken)
    {
        var page = await FetchBySlugAsync(slug, DefaultLang, cancellationToken);
        if (page is null)
        {
            return null;
        }

        var resolvedLang = DefaultLang;
        if (lang != DefaultLang &&
            PolylangFields.Translations(page.Translations).TryGetValue(lang, out var translatedSlug) &&
            await FetchBySlugAsync(translatedSlug, lang, cancellationToken) is { } translated)
        {
            page = translated;
            resolvedLang = lang;
        }

        var (intro, sections) = ParseContent(page.Content?.Rendered ?? string.Empty);
        return new PageContentDto(slug, resolvedLang, ToText(page.Title.Rendered), intro, sections);
    }

    private async Task<WordPressPageDto?> FetchBySlugAsync(string slug, string lang, CancellationToken cancellationToken)
    {
        var uri = $"?slug={Uri.EscapeDataString(slug)}&lang={Uri.EscapeDataString(lang)}&status=publish&_fields=id,slug,title,content,translations";
        using var response = await httpClient.GetAsync(uri, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        var pages = await response.Content.ReadFromJsonAsync<List<WordPressPageDto>>(JsonOptions, cancellationToken) ?? [];
        return pages.FirstOrDefault();
    }

    /// <summary>
    /// Splits the rendered content at each H2: text before the first heading is the intro, every H2
    /// starts a section. Paragraphs are the block elements (p, li, blockquote) reduced to plain text.
    /// </summary>
    public static (IReadOnlyList<string> Intro, IReadOnlyList<PageSectionDto> Sections) ParseContent(string html)
    {
        var parts = H2Regex().Split(html);
        // Split keeps the captured heading text: [intro, h2-1, body-1, h2-2, body-2, ...]
        var intro = Paragraphs(parts[0]);
        var sections = new List<PageSectionDto>();
        for (var i = 1; i + 1 < parts.Length; i += 2)
        {
            sections.Add(new PageSectionDto(ToText(parts[i]), Paragraphs(parts[i + 1])));
        }

        return (intro, sections);
    }

    private static IReadOnlyList<string> Paragraphs(string html)
    {
        return BlockRegex().Matches(html)
            .Select(m => ToText(m.Groups["body"].Value))
            .Where(text => text.Length > 0)
            .ToList();
    }

    private static string ToText(string html)
    {
        var withoutTags = TagRegex().Replace(LineBreakRegex().Replace(html, " "), string.Empty);
        return WhitespaceRegex().Replace(WebUtility.HtmlDecode(withoutTags), " ").Trim();
    }

    [GeneratedRegex(@"<h2\b[^>]*>(.*?)</h2>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex H2Regex();

    [GeneratedRegex(@"<(p|li|blockquote)\b[^>]*>(?<body>.*?)</\1>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex BlockRegex();

    [GeneratedRegex(@"<br\s*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex LineBreakRegex();

    [GeneratedRegex("<[^>]+>", RegexOptions.Singleline)]
    private static partial Regex TagRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}

internal sealed record WordPressPageDto(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("slug")] string Slug,
    [property: JsonPropertyName("title")] WordPressRenderedDto Title,
    [property: JsonPropertyName("content")] WordPressRenderedDto? Content,
    [property: JsonPropertyName("translations")] JsonElement? Translations = null);

/// <summary>Singleton cache of parsed pages (10 min), so WordPress isn't hit on every SSR render.</summary>
public sealed class WordPressPageCache
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(10);
    private readonly ConcurrentDictionary<string, (PageContentDto? Page, DateTimeOffset Expiry)> _entries = new();

    public async Task<PageContentDto?> GetOrAddAsync(
        string key,
        Func<CancellationToken, Task<PageContentDto?>> load,
        CancellationToken cancellationToken)
    {
        if (_entries.TryGetValue(key, out var entry) && DateTimeOffset.UtcNow < entry.Expiry)
        {
            return entry.Page;
        }

        try
        {
            var page = await load(cancellationToken);
            _entries[key] = (page, DateTimeOffset.UtcNow.Add(Ttl));
            return page;
        }
        catch when (entry.Page is not null)
        {
            // WordPress unreachable: keep serving the last good copy.
            return entry.Page;
        }
    }
}
