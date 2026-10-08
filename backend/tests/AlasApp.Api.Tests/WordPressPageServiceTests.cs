using AlasApp.Infrastructure.WordPress;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Text;
using Xunit;

namespace AlasApp.Api.Tests;

public sealed class WordPressPageServiceTests
{
    // Gutenberg output for a page edited in WordPress.
    private const string EsContent = """
        <p>ALAS GLOBAL TOUR es una asociaci&oacute;n fundada en <strong>2001</strong>.</p>
        <h2 class="wp-block-heading">De un torneo de naciones</h2>
        <p>En el a&ntilde;o 2002 se dio inicio&#8230;</p>
        <p>En 2023 inicia<br>ALAS Global Tour.</p>
        <h2 class="wp-block-heading" id="mision">Misión</h2>
        <ul><li>Fortalecer el surf.</li></ul>
        <h2>Visión</h2>
        <p>   </p>
        """;

    [Fact]
    public void ParseContent_SplitsIntroAndSectionsAsPlainText()
    {
        var (intro, sections) = WordPressPageService.ParseContent(EsContent);

        Assert.Equal(["ALAS GLOBAL TOUR es una asociación fundada en 2001."], intro);
        Assert.Equal(3, sections.Count);
        Assert.Equal("De un torneo de naciones", sections[0].Title);
        Assert.Equal(["En el año 2002 se dio inicio…", "En 2023 inicia ALAS Global Tour."], sections[0].Paragraphs);
        Assert.Equal("Misión", sections[1].Title);
        Assert.Equal(["Fortalecer el surf."], sections[1].Paragraphs);
        Assert.Empty(sections[2].Paragraphs);
    }

    [Fact]
    public async Task GetBySlug_UsesPolylangTranslationAndFallsBackToSpanish()
    {
        var handler = new StubHandler(request => request.RequestUri!.Query switch
        {
            var q when q.Contains("slug=quienes-somos") => Json("""
                [{ "id": 1, "slug": "quienes-somos", "title": { "rendered": "Qui&eacute;nes somos" },
                   "content": { "rendered": "<p>Hola</p>" }, "translations": { "es": "quienes-somos", "en": "about-us" } }]
                """),
            var q when q.Contains("slug=about-us") => Json("""
                [{ "id": 2, "slug": "about-us", "title": { "rendered": "About us" }, "content": { "rendered": "<p>Hello</p>" } }]
                """),
            _ => Json("[]"),
        });
        var service = new WordPressPageService(
            new HttpClient(handler) { BaseAddress = new Uri("https://wp.test/wp-json/wp/v2/pages/") },
            new WordPressPageCache(),
            NullLogger<WordPressPageService>.Instance);

        var en = await service.GetBySlugAsync("quienes-somos", "en", CancellationToken.None);
        var pt = await service.GetBySlugAsync("quienes-somos", "pt", CancellationToken.None);
        var missing = await service.GetBySlugAsync("no-existe", "es", CancellationToken.None);

        Assert.Equal("en", en!.Lang);
        Assert.Equal(["Hello"], en.Intro);
        Assert.Equal("es", pt!.Lang);
        Assert.Equal("Quiénes somos", pt.Title);
        Assert.Null(missing);
    }

    private static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(respond(request));
    }
}
