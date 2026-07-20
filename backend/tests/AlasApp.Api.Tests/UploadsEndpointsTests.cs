using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Articles.Models;
using AlasApp.Application.Common;
using AlasApp.Application.Uploads.Models;
using AlasApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Xunit;

namespace AlasApp.Api.Tests;

public sealed class UploadsEndpointsTests : IClassFixture<UploadsWebApplicationFactory>
{
    private readonly HttpClient _client;

    public UploadsEndpointsTests(UploadsWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task UploadEventPoster_ShouldReturnUploadedMedia()
    {
        using var form = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("fake-image-content"));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
        form.Add(fileContent, "file", "poster.png");

        var response = await _client.PostAsync("/v1/uploads/event-poster", form);
        var body = await response.Content.ReadAsStringAsync();

        Assert.True(response.StatusCode == HttpStatusCode.Created, body);

        using var json = JsonDocument.Parse(body);
        Assert.Equal("501", json.RootElement.GetProperty("mediaId").GetString());
        Assert.Equal("https://cdn.test/wp/poster.png", json.RootElement.GetProperty("url").GetString());
        Assert.Equal("poster.png", json.RootElement.GetProperty("fileName").GetString());
        Assert.Equal("image/png", json.RootElement.GetProperty("contentType").GetString());
    }

    [Fact]
    public async Task UploadEventPoster_WithInvalidContentType_ShouldReturnBadRequest()
    {
        using var form = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("not-image"));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
        form.Add(fileContent, "file", "poster.pdf");

        var response = await _client.PostAsync("/v1/uploads/event-poster", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

public sealed class UploadsWebApplicationFactory : CustomWebApplicationFactory
{
    private readonly string _databaseName = $"UploadsTests-{Guid.NewGuid():N}";

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
        services.RemoveAll<IWordPressService>();
        services.AddScoped<IWordPressService, FakeUploadWordPressService>();
    }
}

internal sealed class FakeUploadWordPressService : IWordPressService
{
    public Task<PagedResult<ArticleSummaryDto>> ListArticlesAsync(ArticleListFilter filter, CancellationToken cancellationToken)
        => Task.FromResult(new PagedResult<ArticleSummaryDto>([], 1, 20, 0));

    public Task<ArticleDetailDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
        => Task.FromResult<ArticleDetailDto?>(null);

    public Task<ArticleDetailDto> CreateAsync(ArticleUpsertDto article, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<ArticleDetailDto> UpdateAsync(string slug, ArticleUpsertDto article, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<bool> DeleteAsync(string slug, CancellationToken cancellationToken)
        => Task.FromResult(false);

    public Task<UploadedMediaDto> UploadMediaAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken)
    {
        return Task.FromResult(new UploadedMediaDto("501", $"https://cdn.test/wp/{fileName}", fileName, contentType, content.CanSeek ? content.Length : 0));
    }
}
