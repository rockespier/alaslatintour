using AlasApp.Application.Articles.Models;
using AlasApp.Application.Galleries.Models;
using AlasApp.Domain.Enums;
using AlasApp.Infrastructure.Persistence;
using AlasApp.Infrastructure.WordPress;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text;
using Xunit;

namespace AlasApp.Api.Tests;

public sealed class WordPressAdaptersTests
{
    [Fact]
    public async Task WordPressService_ListArticlesAsync_ShouldMapEmbeddedImageAndTags()
    {
        const string payload = """
        [
          {
            "id": 501,
            "date": "2026-07-09T10:00:00Z",
            "slug": "final-day-highlights",
            "title": { "rendered": "Final Day Highlights" },
            "excerpt": { "rendered": "<p>Resumen <strong>oficial</strong></p>" },
            "content": { "rendered": "<p>Contenido completo</p>" },
            "featured_media": 321,
            "sticky": true,
            "meta": {
              "author_role": "Periodista ALAS",
              "read_time_minutes": 3,
              "show_ranking": true,
              "featured": true,
              "article_category": "Resultados",
              "tags_csv": ""
            },
            "_embedded": {
              "author": [
                { "name": "Equipo ALAS" }
              ],
              "wp:featuredmedia": [
                {
                  "source_url": "https://cdn.test/fallback.jpg",
                  "media_details": {
                    "full": { "source_url": "https://cdn.test/featured-full.jpg" }
                  }
                }
              ],
              "wp:term": [
                [
                  { "id": 1, "name": "Highlights", "taxonomy": "post_tag" },
                  { "id": 2, "name": "ALAS", "taxonomy": "post_tag" }
                ]
              ]
            }
          }
        ]
        """;

        var handler = new StubHttpMessageHandler(_ => CreateJsonResponse(payload, totalItems: 1));
        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test/wp-json/wp/v2/posts/")
        };
        await using var dbContext = CreateDbContext();

        var service = new WordPressService(client, dbContext);

        var result = await service.ListArticlesAsync(new ArticleListFilter(1, 10, null, null, "highlights"), CancellationToken.None);

        var article = Assert.Single(result.Items);
        Assert.Equal("Final Day Highlights", article.Titulo);
        Assert.Equal("Resumen oficial", article.Resumen);
        Assert.Equal(ArticleCategory.Resultados, article.Categoria);
        Assert.Equal("Equipo ALAS", article.Autor);
        Assert.Equal("Periodista ALAS", article.AutorTitulo);
        Assert.Equal("https://cdn.test/featured-full.jpg", article.ImagenUrl);
        Assert.Equal(new[] { "Highlights", "ALAS" }, article.Tags);
        Assert.True(article.Featured);
        Assert.Equal(3, article.TiempoLecturaMin);
        Assert.Equal(1, result.TotalItems);
        Assert.Contains(handler.Requests, request => request.RequestUri?.Query.Contains("_embed=1") == true);
        Assert.Contains(handler.Requests, request => request.RequestUri?.Query.Contains("search=highlights") == true);
    }

    [Fact]
    public async Task GalleryService_GetBySlugAsync_ShouldMapDaysAndCoverFromWordPressPayload()
    {
        const string payload = """
        [
          {
            "id": 29,
            "slug": "roca-bruja-classic",
            "title": { "rendered": "Roca Bruja Classic" },
            "acf": {
              "gallery_days": [
                {
                  "day_name": "1",
                  "photos": [
                    {
                      "id": 30,
                      "url": "https://cdn.test/47636.jpg",
                      "width": 700,
                      "height": 467
                    },
                    {
                      "id": 31,
                      "url": "https://cdn.test/47980.jpg",
                      "width": 700,
                      "height": 467
                    }
                  ]
                },
                {
                  "day_name": "2",
                  "photos": [
                    {
                      "id": 32,
                      "url": "https://cdn.test/48222.jpg",
                      "width": 700,
                      "height": 467
                    }
                  ]
                }
              ],
              "press_download_link": "https://drive.test/roca-bruja",
              "event_date": "20260708"
            }
          }
        ]
        """;

        var handler = new StubHttpMessageHandler(_ => CreateJsonResponse(payload));
        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test/wp-json/wp/v2/gallery/")
        };

        var service = new GalleryService(client);

        var result = await service.GetBySlugAsync("roca-bruja-classic", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("roca-bruja-classic", result!.Slug);
        Assert.Equal("https://cdn.test/47636.jpg", result.CoverImageUrl);
        Assert.Equal(3, result.PhotoCount);
        Assert.Equal("https://drive.test/roca-bruja", result.PressDownloadLink);
        Assert.Equal(2, result.GalleryDays.Count);

        var day1 = result.GalleryDays.First();
        Assert.Equal("1", day1.DayName);
        Assert.Equal(2, day1.Assets.Count);
        Assert.All(day1.Assets, asset => Assert.Equal(GalleryAssetType.Photo, asset.Type));
        Assert.Contains(handler.Requests, request => request.RequestUri?.AbsoluteUri == "https://example.test/wp-json/wp/v2/gallery/");
    }

    private static AlasAppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AlasAppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AlasAppDbContext(options);
    }

    private static HttpResponseMessage CreateJsonResponse(string payload, int? totalItems = null)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

        if (totalItems.HasValue)
        {
            response.Headers.Add("X-WP-Total", totalItems.Value.ToString());
        }

        return response;
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(responder(request));
        }
    }
}
