using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Articles.Models;
using AlasApp.Application.Common;
using AlasApp.Domain.Enums;
using AlasApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace AlasApp.Api.Tests;

public sealed class ArticlesEndpointsTests : IClassFixture<ArticlesWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ArticlesEndpointsTests(ArticlesWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task List_ShouldReturnFilteredArticles()
    {
        var response = await _client.GetAsync("/v1/articles?category=Resultados&featured=true");
        response.EnsureSuccessStatusCode();

        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(1, payload.GetProperty("data").GetArrayLength());
        Assert.Equal("Resultados", payload.GetProperty("data")[0].GetProperty("categoria").GetString());
        Assert.True(payload.GetProperty("data")[0].GetProperty("featured").GetBoolean());
        Assert.Equal("article-1", payload.GetProperty("data")[0].GetProperty("id").GetString());
    }

    [Fact]
    public async Task GetBySlug_ShouldReturnArticleDetailWithHtmlAndMeta()
    {
        var response = await _client.GetAsync("/v1/articles/nota-destacada");
        response.EnsureSuccessStatusCode();

        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("nota-destacada", payload.GetProperty("slug").GetString());
        Assert.Equal("Nota destacada", payload.GetProperty("titulo").GetString());
        Assert.Equal("article-1", payload.GetProperty("id").GetString());
        Assert.Equal("<p>Contenido destacado</p>", payload.GetProperty("content").GetString());
        Assert.True(payload.GetProperty("showRankingWidget").GetBoolean());
        Assert.Equal("tour", payload.GetProperty("tags")[0].GetString());
        Assert.Equal("Periodista ALAS", payload.GetProperty("author").GetProperty("role").GetString());
    }

    [Fact]
    public async Task Create_ShouldReturnCreatedDetail()
    {
        var response = await _client.PostAsJsonAsync("/v1/articles", new
        {
            titulo = "Nueva nota",
            resumen = "Resumen de la nota",
            categoria = "Circuito",
            autor = "Equipo ALAS",
            autorTitulo = "Redacción",
            imagenUrl = "https://cdn.test/image.jpg",
            tags = new[] { "tour", "olas" },
            featured = false,
            relatedEventId = (string?)null,
            content = "<p>Contenido HTML</p>",
            showRankingWidget = false
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("Nueva nota", payload.GetProperty("titulo").GetString());
        Assert.Equal("<p>Contenido HTML</p>", payload.GetProperty("content").GetString());
    }

    [Fact]
    public async Task Update_ShouldReturnUpdatedArticleDetail()
    {
        var response = await _client.PutAsJsonAsync("/v1/articles/nota-destacada", new
        {
            titulo = "Nota actualizada",
            resumen = "Resumen actualizado",
            categoria = "Entrevista",
            autor = "Equipo ALAS",
            autorTitulo = "Editor",
            imagenUrl = "https://cdn.test/updated.jpg",
            tags = new[] { "entrevista" },
            featured = true,
            relatedEventId = (string?)null,
            content = "<p>Contenido actualizado</p>",
            showRankingWidget = false
        });

        response.EnsureSuccessStatusCode();
        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("Nota actualizada", payload.GetProperty("titulo").GetString());
        Assert.Equal("Entrevista", payload.GetProperty("categoria").GetString());
        Assert.Equal("<p>Contenido actualizado</p>", payload.GetProperty("content").GetString());
        Assert.False(payload.GetProperty("showRankingWidget").GetBoolean());
    }

    [Fact]
    public async Task Delete_ShouldReturnNoContent()
    {
        var response = await _client.DeleteAsync("/v1/articles/nota-destacada");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task GetBySlug_WhenMissing_ShouldReturnNotFound()
    {
        var response = await _client.GetAsync("/v1/articles/no-existe");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public sealed class ArticlesWebApplicationFactory : CustomWebApplicationFactory
{
    protected override bool UseRelationalDatabaseInitialization => false;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureServices((_, services) =>
        {
            services.RemoveAll<DbContextOptions<AlasAppDbContext>>();
            services.AddDbContext<AlasAppDbContext>(options => options.UseInMemoryDatabase("ArticlesTests"));
            ConfigureTestServices(services);
        });
    }

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.RemoveAll<IWordPressService>();
        services.AddScoped<IWordPressService, FakeWordPressService>();
    }
}

internal sealed class FakeWordPressService : IWordPressService
{
    private readonly List<ArticleDetailDto> _articles =
    [
        new(
            "article-1",
            "Nota destacada",
            "Resumen destacado",
            "<p>Contenido destacado</p>",
            ArticleCategory.Resultados,
            new ArticleAuthorDto("Equipo ALAS", "Periodista ALAS"),
            "https://cdn.test/nota.jpg",
            ["tour", "ranking"],
            true,
            true,
            null,
            "nota-destacada",
            new DateTimeOffset(2026, 7, 8, 0, 0, 0, TimeSpan.Zero),
            3),
        new(
            "article-2",
            "Nota general",
            "Resumen general",
            "<p>Contenido general</p>",
            ArticleCategory.Circuito,
            new ArticleAuthorDto("Equipo ALAS", "Redacción"),
            "https://cdn.test/general.jpg",
            ["tour"],
            false,
            false,
            null,
            "nota-general",
            new DateTimeOffset(2026, 7, 7, 0, 0, 0, TimeSpan.Zero),
            2)
    ];

    public Task<PagedResult<ArticleSummaryDto>> ListArticlesAsync(ArticleListFilter filter, CancellationToken cancellationToken)
    {
        IEnumerable<ArticleSummaryDto> query = _articles.Select(ToSummary);

        if (filter.Category.HasValue)
        {
            query = query.Where(x => x.Categoria == filter.Category.Value);
        }

        if (filter.Featured.HasValue)
        {
            query = query.Where(x => x.Featured == filter.Featured.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            query = query.Where(x => x.Titulo.Contains(filter.Search, StringComparison.OrdinalIgnoreCase));
        }

        var page = filter.Page <= 0 ? 1 : filter.Page;
        var limit = filter.Limit <= 0 ? 20 : filter.Limit;
        var items = query.ToList();

        return Task.FromResult(new PagedResult<ArticleSummaryDto>(items, page, limit, items.Count));
    }

    public Task<ArticleDetailDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        return Task.FromResult(_articles.FirstOrDefault(x => x.Slug == slug));
    }

    public Task<ArticleDetailDto> CreateAsync(ArticleUpsertDto article, CancellationToken cancellationToken)
    {
        return Task.FromResult(new ArticleDetailDto(
            "article-created",
            article.Titulo,
            article.Resumen,
            article.ContentHtml,
            article.Categoria,
            new ArticleAuthorDto(article.Autor, article.AutorTitulo),
            article.ImagenUrl,
            article.Tags.ToList(),
            article.Featured,
            article.ShowRankingWidget,
            article.RelatedEventId,
            "nueva-nota",
            new DateTimeOffset(2026, 7, 8, 0, 0, 0, TimeSpan.Zero),
            1));
    }

    public Task<ArticleDetailDto> UpdateAsync(string slug, ArticleUpsertDto article, CancellationToken cancellationToken)
    {
        var existing = _articles.FirstOrDefault(x => x.Slug == slug);
        if (existing is null)
        {
            throw new NotFoundException("No se encontró el artículo solicitado.");
        }

        return Task.FromResult(new ArticleDetailDto(
            existing.Id,
            article.Titulo,
            article.Resumen,
            article.ContentHtml,
            article.Categoria,
            new ArticleAuthorDto(article.Autor, article.AutorTitulo),
            article.ImagenUrl,
            article.Tags.ToList(),
            article.Featured,
            article.ShowRankingWidget,
            article.RelatedEventId,
            slug,
            existing.FechaPublicacion,
            existing.TiempoLecturaMin));
    }

    public Task<bool> DeleteAsync(string slug, CancellationToken cancellationToken)
    {
        return Task.FromResult(_articles.RemoveAll(x => x.Slug == slug) > 0);
    }

    private static ArticleSummaryDto ToSummary(ArticleDetailDto article)
    {
        return new ArticleSummaryDto(
            article.Id,
            article.Titulo,
            article.Resumen,
            article.Categoria,
            article.Author.Name,
            article.Author.Role,
            article.ImagenUrl,
            article.Tags.ToList(),
            article.Featured,
            article.RelatedEventId,
            article.Slug,
            article.FechaPublicacion,
            article.TiempoLecturaMin);
    }
}
