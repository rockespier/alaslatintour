using AlasApp.Application.Articles.Models;
using AlasApp.Application.Common;
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
        using var mediaClient = new HttpClient(new StubHttpMessageHandler(_ => CreateJsonResponse("{}")))
        {
            BaseAddress = new Uri("https://example.test/wp-json/wp/v2/media/")
        };
        await using var dbContext = CreateDbContext();

        var service = new WordPressService(client, dbContext, new WordPressMediaService(mediaClient));

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

        var result = await service.GetBySlugAsync("roca-bruja-classic", null, CancellationToken.None);

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

    [Fact]
    public async Task WordPressService_GetBySlugAsync_ShouldExposePolylangTranslations()
    {
        const string payload = """
        [{
          "id": 147, "date": "2026-07-09T10:00:00Z", "slug": "final-stretch",
          "title": { "rendered": "Final stretch" }, "featured_media": 0, "sticky": false,
          "lang": "en",
          "translations": { "es": "recta-final", "en": "final-stretch", "pt": "reta-final" }
        }]
        """;
        var handler = new StubHttpMessageHandler(_ => CreateJsonResponse(payload));
        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test/wp-json/wp/v2/posts/")
        };
        using var mediaClient = new HttpClient(new StubHttpMessageHandler(_ => CreateJsonResponse("{}")))
        {
            BaseAddress = new Uri("https://example.test/wp-json/wp/v2/media/")
        };
        await using var dbContext = CreateDbContext();
        var service = new WordPressService(client, dbContext, new WordPressMediaService(mediaClient));

        var article = await service.GetBySlugAsync("final-stretch", "en", CancellationToken.None);

        Assert.Equal("recta-final", article!.Translations!["es"]);
        Assert.Equal("final-stretch", article.Translations["en"]);
        Assert.Equal("reta-final", article.Translations["pt"]);
    }

    [Theory]
    [InlineData("en", "lang=en")]
    [InlineData(null, null)]
    public async Task WordPressService_ShouldForwardLangOnlyWhenProvided(string? lang, string? expectedQuery)
    {
        var handler = new StubHttpMessageHandler(_ => CreateJsonResponse("[]", totalItems: 0));
        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test/wp-json/wp/v2/posts/")
        };
        using var mediaClient = new HttpClient(new StubHttpMessageHandler(_ => CreateJsonResponse("{}")))
        {
            BaseAddress = new Uri("https://example.test/wp-json/wp/v2/media/")
        };
        await using var dbContext = CreateDbContext();
        var service = new WordPressService(client, dbContext, new WordPressMediaService(mediaClient));

        await service.ListArticlesAsync(new ArticleListFilter(1, 10, null, null, null, lang), CancellationToken.None);
        await service.GetBySlugAsync("final-day-highlights", lang, CancellationToken.None);

        Assert.Equal(2, handler.Requests.Count);
        Assert.All(handler.Requests, request =>
        {
            var query = request.RequestUri!.Query;
            if (expectedQuery is null)
            {
                Assert.DoesNotContain("lang=", query);
            }
            else
            {
                Assert.Contains(expectedQuery, query);
            }
        });
    }

    [Theory]
    [InlineData("pt", "https://example.test/wp-json/wp/v2/gallery/?lang=pt")]
    [InlineData(null, "https://example.test/wp-json/wp/v2/gallery/")]
    public async Task GalleryService_ListAsync_ShouldForwardLangOnlyWhenProvided(string? lang, string expectedUri)
    {
        var handler = new StubHttpMessageHandler(_ => CreateJsonResponse(GalleryPayload(("roca-bruja-en", "\"pt\""))));
        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test/wp-json/wp/v2/gallery/")
        };

        await new GalleryService(client).ListAsync(lang, CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(expectedUri, request.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task GalleryService_ListAsync_ShouldFallBackToUntranslatedOrSpanishWhenLanguageIsEmpty()
    {
        // `gallery` not enabled in Polylang yet: `?lang=en` answers [] and posts carry `"lang": false`.
        var handler = new StubHttpMessageHandler(request => CreateJsonResponse(
            request.RequestUri!.Query.Contains("lang=")
                ? "[]"
                : GalleryPayload(("sin-idioma", "false"), ("en-espanol", "\"es\""), ("em-portugues", "\"pt\""))));
        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test/wp-json/wp/v2/gallery/")
        };

        var result = await new GalleryService(client).ListAsync("en", CancellationToken.None);

        Assert.Equal(new[] { "sin-idioma", "en-espanol" }, result.Select(g => g.Slug));
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task GalleryService_ListAsync_FallbackShouldDeriveLanguageFromTranslationsWhenLangFieldIsMissing()
    {
        // Real WordPress shape (2026-10-04): no `lang` field, only `translations`.
        const string all = """
        [
          { "id": 1, "slug": "dia-4", "title": { "rendered": "Día 4" }, "translations": { "es": "dia-4", "pt": "dia-4-2" } },
          { "id": 2, "slug": "dia-4-2", "title": { "rendered": "Dia 4" }, "translations": { "es": "dia-4", "pt": "dia-4-2" } }
        ]
        """;
        var handler = new StubHttpMessageHandler(request => CreateJsonResponse(
            request.RequestUri!.Query.Contains("lang=") ? "[]" : all));
        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test/wp-json/wp/v2/gallery/")
        };

        var result = await new GalleryService(client).ListAsync("en", CancellationToken.None);

        Assert.Equal(new[] { "dia-4" }, result.Select(g => g.Slug));
    }

    [Theory]
    [InlineData("""{ "es": "dia-4", "en": "day-4", "fr": "jour-4" }""", "es=dia-4,en=day-4")]
    [InlineData("[]", "")]
    public async Task GalleryService_GetBySlugAsync_ShouldExposePolylangTranslations(string translationsJson, string expected)
    {
        var payload = $$"""
        [{ "id": 1, "slug": "dia-4", "title": { "rendered": "Día 4" }, "lang": "es", "translations": {{translationsJson}} }]
        """;
        var handler = new StubHttpMessageHandler(_ => CreateJsonResponse(payload));
        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test/wp-json/wp/v2/gallery/")
        };

        var result = await new GalleryService(client).GetBySlugAsync("dia-4", "es", CancellationToken.None);

        Assert.Equal(expected, string.Join(',', result!.Translations!.Select(t => $"{t.Key}={t.Value}")));
    }

    private static string GalleryPayload(params (string Slug, string LangJson)[] posts)
    {
        return "[" + string.Join(',', posts.Select((p, i) =>
            $$"""{ "id": {{i + 1}}, "slug": "{{p.Slug}}", "title": { "rendered": "{{p.Slug}}" }, "lang": {{p.LangJson}} }""")) + "]";
    }

    [Theory]
    [InlineData("en", "en")]
    [InlineData(" PT ", "pt")]
    [InlineData("es", "es")]
    [InlineData("fr", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void ContentLanguage_Normalize_ShouldOnlyAcceptSiteLanguages(string? input, string? expected)
    {
        Assert.Equal(expected, ContentLanguage.Normalize(input));
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
