using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Videos.Models;
using AlasApp.Infrastructure.Persistence;
using AlasApp.Infrastructure.YouTube;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Xml.Linq;
using Xunit;

namespace AlasApp.Api.Tests;

public sealed class VideosEndpointsTests : IClassFixture<VideosWebApplicationFactory>
{
    private readonly HttpClient _client;

    public VideosEndpointsTests(VideosWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task List_ShouldReturnVideos()
    {
        var response = await _client.GetAsync("/v1/videos");
        response.EnsureSuccessStatusCode();

        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        var first = payload.GetProperty("data")[0];

        Assert.Equal("MVTLcQ6sbaY", first.GetProperty("id").GetString());
        Assert.Equal("Premiación Garabito Pro", first.GetProperty("title").GetString());
        Assert.Equal("https://i2.ytimg.com/vi/MVTLcQ6sbaY/hqdefault.jpg", first.GetProperty("thumbnailUrl").GetString());
        Assert.Equal("https://www.youtube.com/watch?v=MVTLcQ6sbaY", first.GetProperty("url").GetString());
        Assert.Equal(new DateTimeOffset(2026, 8, 24, 23, 50, 31, TimeSpan.Zero), first.GetProperty("publishedAt").GetDateTimeOffset());
    }

    [Fact]
    public void ParseFeed_ShouldMapEntriesNewestFirst()
    {
        const string xml = """
            <feed xmlns:yt="http://www.youtube.com/xml/schemas/2015" xmlns:media="http://search.yahoo.com/mrss/" xmlns="http://www.w3.org/2005/Atom">
              <title>ALAS GLOBAL TOUR</title>
              <entry>
                <yt:videoId>older</yt:videoId>
                <title>Finales</title>
                <published>2026-08-22T21:01:34+00:00</published>
              </entry>
              <entry>
                <yt:videoId>MVTLcQ6sbaY</yt:videoId>
                <title>Premiación Garabito Pro</title>
                <published>2026-08-24T23:50:31+00:00</published>
                <media:group><media:thumbnail url="https://i2.ytimg.com/vi/MVTLcQ6sbaY/hqdefault.jpg" width="480" height="360"/></media:group>
              </entry>
              <entry>
                <title>Sin id</title>
                <published>2026-08-25T00:00:00+00:00</published>
              </entry>
            </feed>
            """;

        var videos = YouTubeVideoService.ParseFeed(XDocument.Parse(xml)).ToList();

        Assert.Equal(2, videos.Count);
        Assert.Equal("MVTLcQ6sbaY", videos[0].Id);
        Assert.Equal("https://i2.ytimg.com/vi/MVTLcQ6sbaY/hqdefault.jpg", videos[0].ThumbnailUrl);
        Assert.Equal("older", videos[1].Id);
        Assert.Equal("https://i.ytimg.com/vi/older/hqdefault.jpg", videos[1].ThumbnailUrl);
        Assert.Equal("https://www.youtube.com/watch?v=older", videos[1].Url);
    }

    [Fact]
    public async Task FeedCache_ShouldServeLastGoodListWhenRefreshFails()
    {
        var cache = new YouTubeFeedCache();
        IReadOnlyCollection<VideoDto> good = [FakeVideoService.Video];

        await cache.GetOrRefreshAsync(_ => Task.FromResult(good), TimeSpan.Zero, CancellationToken.None);
        var result = await cache.GetOrRefreshAsync(
            _ => throw new HttpRequestException("YouTube down"), TimeSpan.Zero, CancellationToken.None);

        Assert.Same(good, result);
    }
}

public sealed class VideosWebApplicationFactory : CustomWebApplicationFactory
{
    private readonly string _databaseName = $"VideosTests-{Guid.NewGuid():N}";

    protected override bool UseRelationalDatabaseInitialization => false;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureServices((_, services) =>
        {
            RemoveDbContextRegistrations(services);
            services.AddDbContext<AlasAppDbContext>(options => options.UseInMemoryDatabase(_databaseName));
            ConfigureTestServices(services);
        });
    }

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.RemoveAll<IVideoService>();
        services.AddScoped<IVideoService, FakeVideoService>();
    }
}

internal sealed class FakeVideoService : IVideoService
{
    public static readonly VideoDto Video = new(
        "MVTLcQ6sbaY",
        "Premiación Garabito Pro",
        new DateTimeOffset(2026, 8, 24, 23, 50, 31, TimeSpan.Zero),
        "https://i2.ytimg.com/vi/MVTLcQ6sbaY/hqdefault.jpg",
        "https://www.youtube.com/watch?v=MVTLcQ6sbaY");

    public Task<IReadOnlyCollection<VideoDto>> ListAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyCollection<VideoDto>>([Video]);
}
