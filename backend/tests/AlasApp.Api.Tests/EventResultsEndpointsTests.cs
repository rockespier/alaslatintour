using AlasApp.Infrastructure.Persistence;
using ClosedXML.Excel;
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
using System.Net.Http.Headers;
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

    [Fact]
    public async Task ResultsTemplate_AndImportXlsx_Work()
    {
        await TestAdminAuthHelper.AuthenticateAsAdminAsync(_client, _factory.Services);

        var circuitId = await CreateCircuitAsync();
        var eventId = await CreateEventAsync(circuitId, prizeAmountUsd: 8000, stars: 4);
        var categoryId = await CreateCategoryAsync();
        await AssignCategoryAsync(eventId, categoryId);
        var competitorOneId = await CreateCompetitorAsync("Ana", "Torres", $"import-one-{Guid.NewGuid():N}@test.com");
        var competitorTwoId = await CreateCompetitorAsync("Luis", "Rivas", $"import-two-{Guid.NewGuid():N}@test.com");
        await CreateInscriptionAsync(competitorOneId, eventId, categoryId);
        await CreateInscriptionAsync(competitorTwoId, eventId, categoryId);

        var templateResponse = await _client.GetAsync($"/v1/events/{eventId}/results/template?categoryId={categoryId}");
        Assert.Equal(HttpStatusCode.OK, templateResponse.StatusCode);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", templateResponse.Content.Headers.ContentType?.MediaType);

        var templateBytes = await templateResponse.Content.ReadAsByteArrayAsync();
        using var templateWorkbook = new XLWorkbook(new MemoryStream(templateBytes));
        var templateSheet = templateWorkbook.Worksheet("Resultados");
        var lastTemplateRow = templateSheet.LastRowUsed()!.RowNumber();
        Assert.Equal(3, lastTemplateRow);

        using var importWorkbook = new XLWorkbook();
        var worksheet = importWorkbook.Worksheets.Add("Resultados");
        WriteRow(worksheet, 1, "CompetidorId", "Competidor", "Pais", "Puesto", "PuntosLiga", "PremioUsd", "HeatOla1", "HeatOla2");

        for (var row = 2; row <= lastTemplateRow; row++)
        {
            var competitorId = templateSheet.Cell(row, 1).GetString();
            var place = competitorId == competitorOneId ? "1°" : "2°";
            WriteRow(worksheet, row, competitorId, "", "", place, "", "", "", "");
        }

        var response = await _client.PostAsync($"/v1/events/{eventId}/results/import?categoryId={categoryId}", CreateExcelForm(importWorkbook, "resultados-import.xlsx"));
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, body);

        var payload = JObject.Parse(body);
        Assert.Equal(2, payload["processedRows"]?.Value<int>());
        Assert.Equal(2, payload["createdCount"]?.Value<int>());
        Assert.Equal(0, payload["errors"]?.Count());

        var getResponse = await _client.GetAsync($"/v1/events/{eventId}/results?categoryId={categoryId}");
        var listed = JObject.Parse(await getResponse.Content.ReadAsStringAsync());
        Assert.Equal(2, listed["data"]?.Count());
        Assert.Contains(listed["data"]!, x => x["competitorId"]?.Value<string>() == competitorOneId && x["place"]?.Value<string>() == "1°");
    }

    private static MultipartFormDataContent CreateExcelForm(XLWorkbook workbook, string fileName)
    {
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(stream.ToArray());
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        form.Add(fileContent, "file", fileName);
        return form;
    }

    private static void WriteRow(IXLWorksheet worksheet, int rowNumber, params string[] values)
    {
        for (var index = 0; index < values.Length; index++)
        {
            worksheet.Cell(rowNumber, index + 1).Value = values[index];
        }
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
            capacidadMaxima = 20,
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
        var competitorId = body["id"]!.Value<string>()!;
        await TestCompetitorLicense.ActivateAsync(_factory.Services, competitorId);
        return competitorId;
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
            reglamento = true,
            riesgosAceptados = true,
            usoImagenAceptado = true
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
