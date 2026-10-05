namespace AlasApp.Infrastructure.YouTube;

public sealed class YouTubeConfig
{
    public const string SectionName = "YouTubeConfig";

    /// <summary>Official channel id (`UC...`), e.g. youtube.com/@alasglobaltour.</summary>
    public string ChannelId { get; init; } = string.Empty;

    /// <summary>Public RSS feed: no API key needed, returns the latest 15 uploads.</summary>
    public string FeedBaseUrl { get; init; } = "https://www.youtube.com/feeds/videos.xml";

    public int CacheMinutes { get; init; } = 30;
}
