using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Galleries.Models;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace AlasApp.Infrastructure.WordPress;

public sealed class GalleryService(HttpClient httpClient) : IGalleryService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyCollection<GallerySummaryDto>> ListAsync(string? lang, CancellationToken cancellationToken)
    {
        var payload = await GetPayloadAsync(lang, cancellationToken);
        return payload.Select(MapSummary).ToList();
    }

    public async Task<GalleryDetailDto?> GetBySlugAsync(string slug, string? lang, CancellationToken cancellationToken)
    {
        var normalizedSlug = slug.Trim();
        var payload = await GetPayloadAsync(lang, cancellationToken);
        var post = payload.FirstOrDefault(x => string.Equals(x.Slug, normalizedSlug, StringComparison.OrdinalIgnoreCase));
        return post is null ? null : MapDetail(post);
    }

    private async Task<IReadOnlyList<WordPressGalleryPostDto>> GetPayloadAsync(string? lang, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(lang))
        {
            return await FetchAsync(string.Empty, cancellationToken);
        }

        var payload = await FetchAsync($"?lang={Uri.EscapeDataString(lang)}", cancellationToken);
        if (payload.Count > 0)
        {
            return payload;
        }

        // Nothing in that language: either the `gallery` post type isn't enabled in Polylang yet
        // (WordPress then answers `?lang=` with an empty list) or no gallery was translated.
        // Photos are language-neutral, so fall back to the untranslated (or Spanish) galleries
        // instead of showing an empty gallery section.
        var all = await FetchAsync(string.Empty, cancellationToken);
        return all
            .Where(post => PolylangFields.Lang(post.Lang, post.Translations, post.Slug) is null or ContentLanguageFallback)
            .ToList();
    }

    private const string ContentLanguageFallback = "es";

    private async Task<IReadOnlyList<WordPressGalleryPostDto>> FetchAsync(string uri, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(uri, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<List<WordPressGalleryPostDto>>(JsonOptions, cancellationToken) ?? [];
    }

    private static GallerySummaryDto MapSummary(WordPressGalleryPostDto post)
    {
        var photos = FlattenPhotos(post);
        var cover = photos.FirstOrDefault();

        return new GallerySummaryDto(
            post.Id.ToString(),
            post.Slug,
            WebUtility.HtmlDecode(post.Title.Rendered).Trim(),
            ParseEventDate(post.Acf?.EventDate),
            cover?.Url,
            photos.Count,
            post.Date);
    }

    private static GalleryDetailDto MapDetail(WordPressGalleryPostDto post)
    {
        var photos = FlattenPhotos(post);
        var cover = photos.FirstOrDefault();

        return new GalleryDetailDto(
            post.Id.ToString(),
            post.Slug,
            WebUtility.HtmlDecode(post.Title.Rendered).Trim(),
            ParseEventDate(post.Acf?.EventDate),
            post.Acf?.PressDownloadLink,
            cover?.Url,
            photos.Count,
            MapDays(post),
            PolylangFields.Translations(post.Translations),
            post.Date);
    }

    private static IReadOnlyCollection<GalleryDayDto> MapDays(WordPressGalleryPostDto post)
    {
        return post.Acf?.GalleryDays?
            .Select(day => new GalleryDayDto(
                string.IsNullOrWhiteSpace(day.DayName) ? "General" : day.DayName.Trim(),
                (day.Photos ?? [])
                    .Select(photo => new GalleryAssetDto(
                        photo.Id.ToString(),
                        GalleryAssetType.Photo,
                        photo.Url,
                        photo.Width,
                        photo.Height,
                        string.IsNullOrWhiteSpace(photo.Caption) ? null : WebUtility.HtmlDecode(photo.Caption).Trim()))
                    .ToList()))
            .ToList() ?? [];
    }

    private static IReadOnlyList<WordPressGalleryPhotoDto> FlattenPhotos(WordPressGalleryPostDto post)
    {
        return post.Acf?.GalleryDays?
            .SelectMany(day => day.Photos ?? [])
            .ToList() ?? [];
    }

    private static DateTimeOffset? ParseEventDate(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               DateTimeOffset.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
            ? parsed
            : null;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var detail = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new InvalidOperationException($"WordPress respondió {(int)response.StatusCode}: {detail}");
    }
}
