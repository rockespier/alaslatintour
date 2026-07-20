using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Articles.Models;
using AlasApp.Application.Common;
using AlasApp.Application.Uploads.Models;
using AlasApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Xunit;

namespace AlasApp.Api.Tests;

public sealed class LiveEndpointsTests : IClassFixture<LiveEndpointsWebApplicationFactory>
{
    private readonly LiveEndpointsWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public LiveEndpointsTests(LiveEndpointsWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetLive_WithoutActiveStream_ShouldReturnNotLive()
    {
        await ResetStoredSettingsAsync();

        var response = await _client.GetAsync("/v1/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = JObject.Parse(await response.Content.ReadAsStringAsync());
        Assert.False(json["isLive"]!.Value<bool>());
        Assert.True(json["event"] is null || json["event"]!.Type == JTokenType.Null);
    }

    [Fact]
    public async Task GetLive_WithActiveStream_ShouldExposeEventAndVideoId()
    {
        await ResetStoredSettingsAsync();
        await TestAdminAuthHelper.AuthenticateAsAdminAsync(_client, _factory.Services);

        var circuitResponse = await _client.PostAsJsonAsync("/v1/circuits", new
        {
            nombre = "Circuito En Vivo",
            temporada = 2026,
            descripcion = "Circuito de prueba",
            region = "Latinoamérica",
            modalidad = "Shortboard",
            estado = "Activo",
            surfScoresCode = "CIR-LIVE-2026"
        });
        Assert.Equal(HttpStatusCode.Created, circuitResponse.StatusCode);
        var circuit = JObject.Parse(await circuitResponse.Content.ReadAsStringAsync());
        var circuitId = circuit["id"]!.Value<string>();

        var eventResponse = await _client.PostAsJsonAsync("/v1/events", new
        {
            nombre = "Roca Bruja Classic",
            circuitId,
            fechaInicio = "2026-08-12",
            fechaFin = "2026-08-15",
            pais = "Perú",
            ciudad = "Chicama",
            playa = "Roca Bruja",
            stars = 6,
            capacidadMaxima = 60,
            prizeAmountUsd = 5000,
            eventType = "Prime",
            accessType = "Abierto",
            estado = "Activo",
            surfScoresCode = "LIVE-ROCA-BRUJA-2026"
        });
        Assert.True(
            eventResponse.StatusCode == HttpStatusCode.Created,
            await eventResponse.Content.ReadAsStringAsync());
        var createdEvent = JObject.Parse(await eventResponse.Content.ReadAsStringAsync());
        var eventId = createdEvent["id"]!.Value<string>();

        var settingsResponse = await _client.GetAsync("/v1/admin/settings");
        var settings = JObject.Parse(await settingsResponse.Content.ReadAsStringAsync());
        settings["live"]!["youTube"]!["active"] = true;
        settings["live"]!["youTube"]!["eventId"] = eventId;
        settings["live"]!["youTube"]!["videoIdOrUrl"] = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
        settings["live"]!["schedulePdfUrl"] = "https://cdn.test/wp/programacion.pdf";

        using var content = new StringContent(settings.ToString(), Encoding.UTF8, "application/json");
        var updateResponse = await _client.PutAsync("/v1/admin/settings", content);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var liveResponse = await _client.GetAsync("/v1/live");
        Assert.Equal(HttpStatusCode.OK, liveResponse.StatusCode);
        var live = JObject.Parse(await liveResponse.Content.ReadAsStringAsync());

        Assert.True(live["isLive"]!.Value<bool>());
        Assert.Equal("Roca Bruja Classic", live["event"]?["nombre"]?.Value<string>());
        Assert.Equal("dQw4w9WgXcQ", live["youTubeVideoId"]?.Value<string>());
        Assert.Equal("https://cdn.test/wp/programacion.pdf", live["schedulePdfUrl"]?.Value<string>());
    }

    [Fact]
    public async Task UploadLiveSchedule_WithoutAuth_ShouldReturnUnauthorized()
    {
        using var form = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("fake-pdf-content"));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
        form.Add(fileContent, "file", "programacion.pdf");

        var response = await _client.PostAsync("/v1/uploads/live-schedule", form);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UploadLiveSchedule_WithAuthAndPdf_ShouldReturnUploadedMedia()
    {
        await TestAdminAuthHelper.AuthenticateAsAdminAsync(_client, _factory.Services);

        using var form = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("fake-pdf-content"));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
        form.Add(fileContent, "file", "programacion.pdf");

        var response = await _client.PostAsync("/v1/uploads/live-schedule", form);
        var body = await response.Content.ReadAsStringAsync();

        Assert.True(response.StatusCode == HttpStatusCode.Created, body);
        var json = JObject.Parse(body);
        Assert.Equal("https://cdn.test/wp/programacion.pdf", json["url"]?.Value<string>());
        Assert.Equal("application/pdf", json["contentType"]?.Value<string>());
    }

    [Fact]
    public async Task UploadLiveSchedule_WithInvalidContentType_ShouldReturnBadRequest()
    {
        await TestAdminAuthHelper.AuthenticateAsAdminAsync(_client, _factory.Services);

        using var form = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("not-a-pdf"));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
        form.Add(fileContent, "file", "programacion.png");

        var response = await _client.PostAsync("/v1/uploads/live-schedule", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task ResetStoredSettingsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AlasAppDbContext>();
        dbContext.SystemSettings.RemoveRange(dbContext.SystemSettings);
        await dbContext.SaveChangesAsync();
    }
}

public sealed class LiveEndpointsWebApplicationFactory : CustomWebApplicationFactory
{
    private readonly string _databaseName = $"LiveEndpointsTests-{Guid.NewGuid():N}";

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
        services.AddScoped<IWordPressService, FakeLiveWordPressService>();
    }
}

internal sealed class FakeLiveWordPressService : IWordPressService
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
        return Task.FromResult(new UploadedMediaDto("601", $"https://cdn.test/wp/{fileName}", fileName, contentType, content.CanSeek ? content.Length : 0));
    }
}
