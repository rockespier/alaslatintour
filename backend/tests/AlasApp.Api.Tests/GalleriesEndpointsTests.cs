using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Galleries.Models;
using AlasApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using Xunit;

namespace AlasApp.Api.Tests;

public sealed class GalleriesEndpointsTests : IClassFixture<GalleriesWebApplicationFactory>
{
    private readonly HttpClient _client;

    public GalleriesEndpointsTests(GalleriesWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task List_ShouldReturnGalleryCardsWithSingleCover()
    {
        var response = await _client.GetAsync("/v1/galleries");
        response.EnsureSuccessStatusCode();

        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        var first = payload.GetProperty("data")[0];

        Assert.Equal("roca-bruja-classic", first.GetProperty("slug").GetString());
        Assert.Equal("Roca Bruja Classic", first.GetProperty("title").GetString());
        Assert.Equal("https://cdn.test/gallery-1-cover.jpg", first.GetProperty("coverImageUrl").GetString());
        Assert.Equal(3, first.GetProperty("photoCount").GetInt32());
        Assert.False(first.TryGetProperty("photos", out _));
    }

    [Fact]
    public async Task GetBySlug_ShouldReturnGalleryDetail()
    {
        var response = await _client.GetAsync("/v1/galleries/roca-bruja-classic");
        response.EnsureSuccessStatusCode();

        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        var firstDay = payload.GetProperty("galleryDays")[0];
        var firstAsset = firstDay.GetProperty("assets")[0];

        Assert.Equal("roca-bruja-classic", payload.GetProperty("slug").GetString());
        Assert.Equal("https://drive.test/roca-bruja", payload.GetProperty("pressDownloadLink").GetString());
        Assert.Equal("https://cdn.test/gallery-1-cover.jpg", payload.GetProperty("coverImageUrl").GetString());
        Assert.Equal(3, payload.GetProperty("photoCount").GetInt32());
        Assert.Equal(2, payload.GetProperty("galleryDays").GetArrayLength());
        Assert.Equal("Day 1", firstDay.GetProperty("dayName").GetString());
        Assert.Equal("photo", firstAsset.GetProperty("type").GetString());
        Assert.Equal("https://cdn.test/gallery-1-cover.jpg", firstAsset.GetProperty("url").GetString());
        Assert.False(payload.TryGetProperty("photos", out _));
    }

    [Fact]
    public async Task GetBySlug_WhenMissing_ShouldReturnNotFound()
    {
        var response = await _client.GetAsync("/v1/galleries/no-existe");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public sealed class GalleriesWebApplicationFactory : CustomWebApplicationFactory
{
    private readonly string _databaseName = $"GalleriesTests-{Guid.NewGuid():N}";

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
        services.RemoveAll<IGalleryService>();
        services.AddScoped<IGalleryService, FakeGalleryService>();
    }
}

internal sealed class FakeGalleryService : IGalleryService
{
    private static readonly List<GalleryDetailDto> Galleries =
    [
        new(
            "gallery-1",
            "roca-bruja-classic",
            "Roca Bruja Classic",
            new DateTimeOffset(2026, 7, 8, 0, 0, 0, TimeSpan.Zero),
            "https://drive.test/roca-bruja",
            "https://cdn.test/gallery-1-cover.jpg",
            3,
            [
                new GalleryDayDto(
                    "Day 1",
                    [
                        new GalleryAssetDto("photo-1", GalleryAssetType.Photo, "https://cdn.test/gallery-1-cover.jpg", 700, 467),
                        new GalleryAssetDto("photo-2", GalleryAssetType.Photo, "https://cdn.test/gallery-1-2.jpg", 700, 467)
                    ]),
                new GalleryDayDto(
                    "Day 2",
                    [
                        new GalleryAssetDto("photo-3", GalleryAssetType.Photo, "https://cdn.test/gallery-1-3.jpg", 700, 467)
                    ])
            ]),
        new(
            "gallery-2",
            "sayulita-masters",
            "Sayulita Masters",
            new DateTimeOffset(2026, 9, 20, 0, 0, 0, TimeSpan.Zero),
            "https://drive.test/sayulita",
            "https://cdn.test/gallery-2-cover.jpg",
            2,
            [
                new GalleryDayDto(
                    "Final",
                    [
                        new GalleryAssetDto("photo-4", GalleryAssetType.Photo, "https://cdn.test/gallery-2-cover.jpg", 900, 600),
                        new GalleryAssetDto("photo-5", GalleryAssetType.Photo, "https://cdn.test/gallery-2-2.jpg", 900, 600)
                    ])
            ])
    ];

    public Task<IReadOnlyCollection<GallerySummaryDto>> ListAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyCollection<GallerySummaryDto>>(
            Galleries.Select(g => new GallerySummaryDto(
                g.Id,
                g.Slug,
                g.Title,
                g.EventDate,
                g.CoverImageUrl,
                g.PhotoCount)).ToList());
    }

    public Task<GalleryDetailDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        return Task.FromResult(Galleries.FirstOrDefault(x => x.Slug == slug));
    }
}
