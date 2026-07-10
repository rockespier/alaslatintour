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

    public async Task<IReadOnlyCollection<GallerySummaryDto>> ListAsync(CancellationToken cancellationToken)
    {
        var payload = await GetPayloadAsync(cancellationToken);
        return payload.Select(MapSummary).ToList();
    }

    public async Task<GalleryDetailDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        var normalizedSlug = slug.Trim();
        var payload = await GetPayloadAsync(cancellationToken);
        var post = payload.FirstOrDefault(x => string.Equals(x.Slug, normalizedSlug, StringComparison.OrdinalIgnoreCase));
        return post is null ? null : MapDetail(post);
    }

    private async Task<IReadOnlyList<WordPressGalleryPostDto>> GetPayloadAsync(CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(string.Empty, cancellationToken);
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
            photos.Count);
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
            MapDays(post));
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
                        photo.Height))
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
