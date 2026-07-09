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

    public async Task<IReadOnlyCollection<GalleryDto>> ListAsync(CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(string.Empty, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<List<WordPressGalleryPostDto>>(JsonOptions, cancellationToken) ?? [];
        return payload.Select(Map).ToList();
    }

    private static GalleryDto Map(WordPressGalleryPostDto post)
    {
        var photos = post.Acf?.GalleryDays?
            .SelectMany(day => day.Photos ?? [])
            .Select(p => new GalleryPhotoDto(p.Id.ToString(), p.Url, p.Width, p.Height))
            .ToList() ?? [];

        return new GalleryDto(
            post.Id.ToString(),
            post.Slug,
            WebUtility.HtmlDecode(post.Title.Rendered).Trim(),
            ParseEventDate(post.Acf?.EventDate),
            post.Acf?.PressDownloadLink,
            photos);
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
