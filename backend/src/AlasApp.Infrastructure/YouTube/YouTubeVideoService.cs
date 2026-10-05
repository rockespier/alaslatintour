using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Videos.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace AlasApp.Infrastructure.YouTube;

/// <summary>Reads the official channel's public RSS feed (no API key) and caches it.</summary>
public sealed class YouTubeVideoService(
    HttpClient httpClient,
    YouTubeFeedCache cache,
    IOptions<YouTubeConfig> options,
    ILogger<YouTubeVideoService> logger) : IVideoService
{
    private static readonly XNamespace Atom = "http://www.w3.org/2005/Atom";
    private static readonly XNamespace Yt = "http://www.youtube.com/xml/schemas/2015";
    private static readonly XNamespace Media = "http://search.yahoo.com/mrss/";

    public async Task<IReadOnlyCollection<VideoDto>> ListAsync(CancellationToken cancellationToken)
    {
        var config = options.Value;
        if (string.IsNullOrWhiteSpace(config.ChannelId))
        {
            return [];
        }

        try
        {
            return await cache.GetOrRefreshAsync(
                ct => FetchAsync(config, ct),
                TimeSpan.FromMinutes(Math.Max(1, config.CacheMinutes)),
                cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or XmlException)
        {
            logger.LogWarning(ex, "Could not read the YouTube feed for channel {ChannelId}", config.ChannelId);
            return [];
        }
    }

    private async Task<IReadOnlyCollection<VideoDto>> FetchAsync(YouTubeConfig config, CancellationToken cancellationToken)
    {
        var url = $"{config.FeedBaseUrl}?channel_id={Uri.EscapeDataString(config.ChannelId)}";
        using var response = await httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var feed = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);
        return ParseFeed(feed);
    }

    public static IReadOnlyCollection<VideoDto> ParseFeed(XDocument feed)
    {
        return feed.Root?
            .Elements(Atom + "entry")
            .Select(MapEntry)
            .OfType<VideoDto>()
            .OrderByDescending(v => v.PublishedAt)
            .ToList() ?? [];
    }

    private static VideoDto? MapEntry(XElement entry)
    {
        var id = entry.Element(Yt + "videoId")?.Value.Trim();
        var title = entry.Element(Atom + "title")?.Value.Trim();
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(title) ||
            !DateTimeOffset.TryParse(entry.Element(Atom + "published")?.Value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var published))
        {
            return null;
        }

        var thumbnail = entry.Element(Media + "group")?.Element(Media + "thumbnail")?.Attribute("url")?.Value
            ?? $"https://i.ytimg.com/vi/{id}/hqdefault.jpg";

        return new VideoDto(id, title, published, thumbnail, $"https://www.youtube.com/watch?v={id}");
    }
}
