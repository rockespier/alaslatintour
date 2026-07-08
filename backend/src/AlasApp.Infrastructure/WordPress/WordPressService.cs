using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Articles.Models;
using AlasApp.Application.Common;
using AlasApp.Domain.Enums;
using AlasApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AlasApp.Infrastructure.WordPress;

public sealed partial class WordPressService(HttpClient httpClient, AlasAppDbContext dbContext) : IWordPressService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [GeneratedRegex("<.*?>", RegexOptions.Compiled | RegexOptions.Singleline)]
    private static partial Regex HtmlTagsRegex();

    public async Task<PagedResult<ArticleSummaryDto>> ListArticlesAsync(ArticleListFilter filter, CancellationToken cancellationToken)
    {
        var page = filter.Page <= 0 ? 1 : filter.Page;
        var limit = filter.Limit <= 0 ? 20 : filter.Limit;

        var query = new List<string>
        {
            "_embed=1",
            $"page={page}",
            $"per_page={limit}"
        };

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            query.Add($"search={Uri.EscapeDataString(filter.Search.Trim())}");
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, BuildRelativeUri(query));
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<List<WordPressPostDto>>(JsonOptions, cancellationToken) ?? [];
        var totalItems = ReadHeader(response, "X-WP-Total", payload.Count);

        var items = await MapAndFilterAsync(payload, filter, cancellationToken);
        return new PagedResult<ArticleSummaryDto>(items, page, limit, totalItems > 0 ? totalItems : items.Count);
    }

    public async Task<ArticleDetailDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(BuildRelativeUri(["_embed=1", $"slug={Uri.EscapeDataString(slug)}"]), cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<List<WordPressPostDto>>(JsonOptions, cancellationToken) ?? [];
        var post = payload.FirstOrDefault();
        return post is null ? null : await MapDetailAsync(post, cancellationToken);
    }

    public async Task<ArticleDetailDto> CreateAsync(ArticleUpsertDto article, CancellationToken cancellationToken)
    {
        var request = ToWordPressPayload(article, GenerateSlug(article.Titulo));

        using var response = await httpClient.PostAsJsonAsync(BuildRelativeUri([]), request, JsonOptions, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<WordPressPostDto>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("WordPress no devolvió el artículo creado.");

        return await MapDetailAsync(payload, cancellationToken);
    }

    public async Task<ArticleDetailDto> UpdateAsync(string slug, ArticleUpsertDto article, CancellationToken cancellationToken)
    {
        var existing = await GetRawBySlugAsync(slug, cancellationToken)
            ?? throw new NotFoundException("No se encontró el artículo solicitado.");

        var request = ToWordPressPayload(article, existing.Slug);
        using var response = await httpClient.PutAsJsonAsync(BuildRelativeUri([$"{existing.Id}"]), request, JsonOptions, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<WordPressPostDto>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("WordPress no devolvió el artículo actualizado.");

        return await MapDetailAsync(payload, cancellationToken);
    }

    public async Task<bool> DeleteAsync(string slug, CancellationToken cancellationToken)
    {
        var existing = await GetRawBySlugAsync(slug, cancellationToken);
        if (existing is null)
        {
            return false;
        }

        using var response = await httpClient.DeleteAsync(BuildRelativeUri([$"{existing.Id}", "force=true"]), cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return true;
    }

    private async Task<List<ArticleSummaryDto>> MapAndFilterAsync(
        IReadOnlyCollection<WordPressPostDto> payload,
        ArticleListFilter filter,
        CancellationToken cancellationToken)
    {
        var mapped = new List<ArticleSummaryDto>(payload.Count);
        foreach (var item in payload)
        {
            mapped.Add(await MapSummaryAsync(item, cancellationToken));
        }

        IEnumerable<ArticleSummaryDto> query = mapped;

        if (filter.Category.HasValue)
        {
            query = query.Where(x => x.Categoria == filter.Category.Value);
        }

        if (filter.Featured.HasValue)
        {
            query = query.Where(x => x.Featured == filter.Featured.Value);
        }

        return query.ToList();
    }

    private async Task<ArticleSummaryDto> MapSummaryAsync(WordPressPostDto post, CancellationToken cancellationToken)
    {
        var mapped = await MapCoreAsync(post, cancellationToken);
        return new ArticleSummaryDto(
            mapped.Id,
            mapped.Titulo,
            mapped.Resumen,
            mapped.Categoria,
            mapped.Author.Name,
            mapped.Author.Role,
            mapped.ImagenUrl,
            mapped.Tags,
            mapped.Featured,
            mapped.RelatedEventId,
            mapped.Slug,
            mapped.FechaPublicacion,
            mapped.TiempoLecturaMin);
    }

    private async Task<ArticleDetailDto> MapDetailAsync(WordPressPostDto post, CancellationToken cancellationToken)
    {
        var mapped = await MapCoreAsync(post, cancellationToken);
        return new ArticleDetailDto(
            mapped.Id,
            mapped.Titulo,
            mapped.Resumen,
            mapped.ContentHtml,
            mapped.Categoria,
            mapped.Author,
            mapped.ImagenUrl,
            mapped.Tags,
            mapped.Featured,
            mapped.ShowRankingWidget,
            mapped.RelatedEventId,
            mapped.Slug,
            mapped.FechaPublicacion,
            mapped.TiempoLecturaMin);
    }

    private async Task<MappedWordPressArticle> MapCoreAsync(WordPressPostDto post, CancellationToken cancellationToken)
    {
        var contentHtml = NormalizeHtml(post.Content?.Rendered);
        var summary = NormalizeText(post.Excerpt?.Rendered) ?? NormalizeText(post.Content?.Rendered) ?? string.Empty;
        var relatedEventId = post.Meta?.RelatedEventId;
        var category = ParseArticleCategory(post.Meta?.ArticleCategory);
        var imageUrl = ResolveImageUrl(post);
        var tags = ResolveTags(post);
        var authorName = post.Embedded?.Author?.FirstOrDefault()?.Name ?? "Equipo ALAS";
        var authorRole = post.Meta?.AuthorRole ?? "Redactor";

        if (!string.IsNullOrWhiteSpace(relatedEventId) &&
            Guid.TryParse(relatedEventId, out var eventId))
        {
            var exists = await dbContext.Events.AsNoTracking().AnyAsync(x => x.Id == eventId, cancellationToken);
            if (!exists)
            {
                relatedEventId = null;
            }
        }

        return new MappedWordPressArticle(
            post.Id.ToString(),
            HtmlDecode(post.Title.Rendered) ?? string.Empty,
            summary,
            contentHtml,
            category,
            new ArticleAuthorDto(authorName, authorRole),
            imageUrl,
            tags,
            post.Sticky,
            post.Meta?.ShowRanking ?? false,
            relatedEventId,
            post.Slug,
            post.Date,
            ContentMetricsHelper.CalculateReadTime(post.Content?.Rendered ?? post.Excerpt?.Rendered ?? string.Empty));
    }

    private async Task<WordPressPostDto?> GetRawBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(BuildRelativeUri(["_embed=1", $"slug={Uri.EscapeDataString(slug)}"]), cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<List<WordPressPostDto>>(JsonOptions, cancellationToken) ?? [];
        return payload.FirstOrDefault();
    }

    private static WordPressCreateUpdateRequest ToWordPressPayload(ArticleUpsertDto article, string slug)
    {
        return new WordPressCreateUpdateRequest(
            article.Titulo,
            article.Resumen,
            article.ContentHtml,
            slug,
            "publish",
            article.Featured,
            new WordPressMutableMetaDto(
                article.AutorTitulo,
                article.ShowRankingWidget,
                ToCategoryText(article.Categoria),
                article.RelatedEventId,
                article.ImagenUrl,
                string.Join(',', article.Tags)));
    }

    private static string BuildRelativeUri(IReadOnlyCollection<string> segments)
    {
        if (segments.Count == 0)
        {
            return string.Empty;
        }

        var pathSegments = segments.Where(x => !x.Contains('=')).ToList();
        var querySegments = segments.Where(x => x.Contains('=')).ToList();
        var path = pathSegments.Count > 0 ? string.Join('/', pathSegments) : string.Empty;

        return querySegments.Count > 0
            ? $"{path}?{string.Join('&', querySegments)}"
            : path;
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

    private static int ReadHeader(HttpResponseMessage response, string name, int fallback)
    {
        return response.Headers.TryGetValues(name, out var values) &&
               int.TryParse(values.FirstOrDefault(), out var parsed)
            ? parsed
            : fallback;
    }

    private static string GenerateSlug(string title)
    {
        return new string(title
                .Trim()
                .ToLowerInvariant()
                .Normalize(System.Text.NormalizationForm.FormD)
                .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                .Select(c => char.IsLetterOrDigit(c) ? c : '-')
                .ToArray())
            .Replace("--", "-", StringComparison.Ordinal)
            .Trim('-');
    }

    private static IReadOnlyCollection<string> ParseTags(string? tagsCsv)
    {
        return string.IsNullOrWhiteSpace(tagsCsv)
            ? []
            : tagsCsv
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
    }

    private static string ResolveImageUrl(WordPressPostDto post)
    {
        if (!string.IsNullOrWhiteSpace(post.Meta?.ImageUrl))
        {
            return post.Meta.ImageUrl;
        }

        var featuredMedia = post.Embedded?.FeaturedMedia?.FirstOrDefault();
        return featuredMedia?.MediaDetails?.Full?.SourceUrl
            ?? featuredMedia?.MediaDetails?.Large?.SourceUrl
            ?? featuredMedia?.MediaDetails?.MediumLarge?.SourceUrl
            ?? featuredMedia?.MediaDetails?.Medium?.SourceUrl
            ?? featuredMedia?.SourceUrl
            ?? string.Empty;
    }

    private static IReadOnlyCollection<string> ResolveTags(WordPressPostDto post)
    {
        var metaTags = ParseTags(post.Meta?.TagsCsv);
        if (metaTags.Count > 0)
        {
            return metaTags;
        }

        var embeddedTags = post.Embedded?.Terms?
            .SelectMany(group => group)
            .Where(term => string.Equals(term.Taxonomy, "post_tag", StringComparison.OrdinalIgnoreCase))
            .Select(term => term.Name?.Trim())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return embeddedTags is { Count: > 0 } ? embeddedTags : [];
    }

    private static string NormalizeHtml(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

    private static string? NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var decoded = HtmlDecode(value);
        if (string.IsNullOrWhiteSpace(decoded))
        {
            return null;
        }

        return HtmlTagsRegex().Replace(decoded, string.Empty).Trim();
    }

    private static string? HtmlDecode(string? value) => string.IsNullOrWhiteSpace(value) ? null : WebUtility.HtmlDecode(value).Trim();

    private static ArticleCategory ParseArticleCategory(string? value)
    {
        return value?.Trim() switch
        {
            "Resultados" => ArticleCategory.Resultados,
            "Circuito" => ArticleCategory.Circuito,
            "Entrevista" => ArticleCategory.Entrevista,
            "Reglamento" => ArticleCategory.Reglamento,
            "Tecnología" => ArticleCategory.Tecnologia,
            _ => ArticleCategory.Circuito
        };
    }

    private static string ToCategoryText(ArticleCategory value)
    {
        return value switch
        {
            ArticleCategory.Resultados => "Resultados",
            ArticleCategory.Circuito => "Circuito",
            ArticleCategory.Entrevista => "Entrevista",
            ArticleCategory.Reglamento => "Reglamento",
            ArticleCategory.Tecnologia => "Tecnología",
            _ => "Circuito"
        };
    }

    private sealed record MappedWordPressArticle(
        string Id,
        string Titulo,
        string Resumen,
        string ContentHtml,
        ArticleCategory Categoria,
        ArticleAuthorDto Author,
        string ImagenUrl,
        IReadOnlyCollection<string> Tags,
        bool Featured,
        bool ShowRankingWidget,
        string? RelatedEventId,
        string Slug,
        DateTimeOffset FechaPublicacion,
        int TiempoLecturaMin);
}
