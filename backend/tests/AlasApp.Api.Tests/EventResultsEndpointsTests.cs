using AlasApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace AlasApp.Api.Tests;

public sealed class EventResultsEndpointsTests : IClassFixture<EventResultsWebApplicationFactory>
{
    private readonly EventResultsWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public EventResultsEndpointsTests(EventResultsWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ResultsEndpoints_ShouldPersistAndReturnEventCategoryResults()
    {
        await TestAdminAuthHelper.AuthenticateAsAdminAsync(_client, _factory.Services);

        var circuitId = await CreateCircuitAsync();
        var eventId = await CreateEventAsync(circuitId, prizeAmountUsd: 10000, stars: 6);
        var categoryId = await CreateCategoryAsync();
        await AssignCategoryAsync(eventId, categoryId);
        var competitorOneId = await CreateCompetitorAsync("Gabriel", "Villani", $"result-one-{Guid.NewGuid():N}@test.com");
        var competitorTwoId = await CreateCompetitorAsync("Mario", "Costa", $"result-two-{Guid.NewGuid():N}@test.com");
        await CreateInscriptionAsync(competitorOneId, eventId, categoryId);
        await CreateInscriptionAsync(competitorTwoId, eventId, categoryId);

        var postResponse = await _client.PostAsJsonAsync($"/v1/events/{eventId}/results", new
        {
            categoryId,
            results = new[]
            {
                new
                {
                    competitorId = competitorOneId,
                    place = "1°",
                    ligaPoints = 0,
                    prizeUsd = (decimal?)null,
                    heatOla1 = 8.50m,
                    heatOla2 = 9.00m
                },
                new
                {
                    competitorId = competitorTwoId,
                    place = "2°",
                    ligaPoints = 760,
                    prizeUsd = (decimal?)2500m,
                    heatOla1 = 7.50m,
                    heatOla2 = 8.00m
                }
            }
        });

        var postBody = await postResponse.Content.ReadAsStringAsync();
        Assert.True(postResponse.StatusCode == HttpStatusCode.Created, postBody);

        var created = JObject.Parse(postBody);
        Assert.Equal(2, created["data"]?.Count());
        Assert.Equal(6000, created["data"]?[0]?["ligaPoints"]?.Value<int>());
        Assert.Equal(4500m, created["data"]?[0]?["prizeUsd"]?.Value<decimal>());
        Assert.Equal(17.5m, created["data"]?[0]?["heatScoreTotal"]?.Value<decimal>());

        var getResponse = await _client.GetAsync($"/v1/events/{eventId}/results?categoryId={categoryId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var listed = JObject.Parse(await getResponse.Content.ReadAsStringAsync());
        Assert.Equal("Results by SurfScores.com", listed["attribution"]?.Value<string>());
        Assert.Equal(2, listed["data"]?.Count());
    }

    [Fact]
    public async Task PrizeDistribution_ShouldUseEventPrizeAndConfiguredStars()
    {
        await TestAdminAuthHelper.AuthenticateAsAdminAsync(_client, _factory.Services);

        var circuitId = await CreateCircuitAsync();
        var eventId = await CreateEventAsync(circuitId, prizeAmountUsd: 20000, stars: 7);

        var response = await _client.GetAsync($"/v1/events/{eventId}/prize-distribution");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = JObject.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(7, json["stars"]?.Value<int>());
        Assert.Equal("1°", json["data"]?[0]?["placeLabel"]?.Value<string>());
        Assert.Equal(9000m, json["data"]?[0]?["prizeUsd"]?.Value<decimal>());
    }

    [Fact]
    public async Task UpsertResults_WithCompetitorNotRegistered_ShouldReturnBadRequest()
    {
        await TestAdminAuthHelper.AuthenticateAsAdminAsync(_client, _factory.Services);

        var circuitId = await CreateCircuitAsync();
        var eventId = await CreateEventAsync(circuitId, prizeAmountUsd: 10000);
        var categoryId = await CreateCategoryAsync();
        await AssignCategoryAsync(eventId, categoryId);
        var competitorId = await CreateCompetitorAsync("No", "Inscrito", $"not-registered-{Guid.NewGuid():N}@test.com");

        var response = await _client.PostAsJsonAsync($"/v1/events/{eventId}/results", new
        {
            categoryId,
            results = new[]
            {
                new
                {
                    competitorId,
                    place = "1°",
                    ligaPoints = 500
                }
            }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<string> CreateCircuitAsync()
    {
        var response = await _client.PostAsJsonAsync("/v1/circuits", new
        {
            nombre = $"Circuito Results {Guid.NewGuid():N}"[..28],
            temporada = 2026,
            descripcion = "Circuito resultados",
            region = "Latinoamérica",
            modalidad = "Shortboard",
            estado = "Activo",
            surfScoresCode = $"RES-{Guid.NewGuid():N}".Substring(0, 12)
        });

        var body = JObject.Parse(await response.Content.ReadAsStringAsync());
        return body["id"]!.Value<string>()!;
    }

    private async Task<string> CreateEventAsync(string circuitId, decimal prizeAmountUsd, int stars = 3)
    {
        var response = await _client.PostAsJsonAsync("/v1/events", new
        {
            nombre = $"Evento Results {Guid.NewGuid():N}"[..25],
            circuitId,
            fechaInicio = "2026-10-02",
            fechaFin = "2026-10-04",
            pais = "Peru",
            ciudad = "Lima",
            playa = "Punta Rocas",
            stars,
            capacidadMaxima = 150,
            prizeAmountUsd,
            surfScoresCode = $"EV-{Guid.NewGuid():N}".Substring(0, 12),
            accessType = "Abierto",
            estado = "Activo"
        });

        var body = JObject.Parse(await response.Content.ReadAsStringAsync());
        return body["id"]!.Value<string>()!;
    }

    private async Task<string> CreateCategoryAsync()
    {
        var response = await _client.PostAsJsonAsync("/v1/categories", new
        {
            nombre = $"OpenResults{Guid.NewGuid():N}"[..18],
            descripcion = "Categoria resultados",
            gender = "Ambos",
            ageRestriction = false,
            minAge = (int?)null,
            maxAge = (int?)null,
            successorCategoryId = (string?)null,
            status = "Activo"
        });

        var body = JObject.Parse(await response.Content.ReadAsStringAsync());
        return body["id"]!.Value<string>()!;
    }

    private async Task AssignCategoryAsync(string eventId, string categoryId)
    {
        var response = await _client.PutAsJsonAsync($"/v1/events/{eventId}/categories", new
        {
            useCircuitTariffs = false,
            categories = new[]
            {
                new
                {
                    categoryId,
                    customTariffUsd = 95,
                    capacidad = 20
                }
            }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<string> CreateCompetitorAsync(string nombre, string apellido, string email)
    {
        var response = await _client.PostAsJsonAsync("/v1/competitors", new
        {
            nombre,
            apellido,
            email,
            fechaNacimiento = "1998-03-08",
            genero = "Masculino",
            pais = "Peru",
            telefono = "+51 999 111 222",
            club = "Club Results",
            postura = "Regular",
            tallaCamiseta = "M",
            numeroCamiseta = "10",
            federacion = "FENTA",
            patrocinadores = "Marca Test"
        });

        var body = JObject.Parse(await response.Content.ReadAsStringAsync());
        return body["id"]!.Value<string>()!;
    }

    private async Task CreateInscriptionAsync(string competitorId, string eventId, string categoryId)
    {
        var response = await _client.PostAsJsonAsync("/v1/inscriptions", new
        {
            competitorId,
            eventId,
            categoryId,
            shirtNumber = "21",
            paymentMethod = "paypal",
            reglamento = true
        });

        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Created, body);
    }
}

public sealed class EventResultsWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"EventResultsTests-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureServices((_, services) =>
        {
            services.RemoveAll<AlasAppDbContext>();
            services.RemoveAll<DbContextOptions<AlasAppDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<IConfigureOptions<DbContextOptions<AlasAppDbContext>>>();
            services.RemoveAll<IConfigureOptions<DbContextOptions>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AlasAppDbContext>>();
            services.AddDbContext<AlasAppDbContext>(options => options.UseInMemoryDatabase(_databaseName));
        });
    }
}
