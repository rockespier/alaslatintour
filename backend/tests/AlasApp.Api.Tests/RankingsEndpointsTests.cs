using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace AlasApp.Api.Tests;

public sealed class RankingsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RankingsEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SyncAndReadRankings_Works_EndToEnd()
    {
        var circuitId = await CreateCircuitAsync();
        var categoryId = await CreateCategoryAsync();
        var eventId = await CreateEventAsync(circuitId);

        await AssignCategoryToEventAsync(eventId, categoryId);

        var competitor1 = await CreateCompetitorAsync("Sofia", "Perez", "Peru");
        var competitor2 = await CreateCompetitorAsync("Ana", "Gomez", "Chile");

        await CreatePaidInscriptionAsync(competitor1, eventId, categoryId);
        await CreatePaidInscriptionAsync(competitor2, eventId, categoryId);
        await UpsertEventResultsAsync(
            eventId,
            categoryId,
            (competitor1, "1", 400),
            (competitor2, "2", 320));

        var syncResponse = await _client.PostAsync($"/v1/surfscores/sync/{circuitId}", null);
        var syncBody = await syncResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, syncResponse.StatusCode);

        using var syncJson = JsonDocument.Parse(syncBody);
        Assert.Equal("ALAS-RANK-26", syncJson.RootElement.GetProperty("circuitCode").GetString());
        Assert.True(syncJson.RootElement.GetProperty("recordsUpdated").GetInt32() >= 0);

        var rankingResponse = await _client.GetAsync($"/v1/rankings?categoryId={categoryId}&year=2026&page=1&limit=10");
        var rankingBody = await rankingResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, rankingResponse.StatusCode);

        using var rankingJson = JsonDocument.Parse(rankingBody);
        Assert.Equal("Results by SurfScores.com", rankingJson.RootElement.GetProperty("attribution").GetString());
        Assert.Equal(categoryId, rankingJson.RootElement.GetProperty("categoryId").GetString());
        Assert.Equal(2026, rankingJson.RootElement.GetProperty("year").GetInt32());
        Assert.Equal(2, rankingJson.RootElement.GetProperty("data").GetArrayLength());

        var categoriesResponse = await _client.GetAsync("/v1/rankings/categories");
        var categoriesBody = await categoriesResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, categoriesResponse.StatusCode);

        using var categoriesJson = JsonDocument.Parse(categoriesBody);
        var categories = categoriesJson.RootElement.GetProperty("data");
        Assert.True(categories.GetArrayLength() >= 1);
        Assert.Contains(categories.EnumerateArray(), x => x.GetProperty("id").GetString() == categoryId);
    }

    [Fact]
    public async Task GetRanking_ShouldUseCurrentSeasonCircuitOnly()
    {
        var currentCircuitId = await CreateCircuitAsync("Ranking Current", "ALAS-RANK-CURRENT-26", "Activo");
        var archivedCircuitId = await CreateCircuitAsync("Ranking Archived", "ALAS-RANK-ARCHIVE-26", "Archivado");
        var categoryId = await CreateCategoryAsync();

        var currentEventId = await CreateEventAsync(currentCircuitId, "Current Event", "RANK-CURRENT-26");
        var archivedEventId = await CreateEventAsync(archivedCircuitId, "Archived Event", "RANK-ARCHIVE-26");

        await AssignCategoryToEventAsync(currentEventId, categoryId);
        await AssignCategoryToEventAsync(archivedEventId, categoryId);

        var currentCompetitor = await CreateCompetitorAsync("Lucia", "Current", "Peru");
        var archivedCompetitor = await CreateCompetitorAsync("Maria", "Archived", "Chile");

        await CreatePaidInscriptionAsync(currentCompetitor, currentEventId, categoryId);
        await CreatePaidInscriptionAsync(archivedCompetitor, archivedEventId, categoryId);
        await UpsertEventResultsAsync(currentEventId, categoryId, (currentCompetitor, "1", 500));
        await UpsertEventResultsAsync(archivedEventId, categoryId, (archivedCompetitor, "1", 900));

        var syncCurrentResponse = await _client.PostAsync($"/v1/surfscores/sync/{currentCircuitId}", null);
        Assert.Equal(HttpStatusCode.OK, syncCurrentResponse.StatusCode);

        var syncArchivedResponse = await _client.PostAsync($"/v1/surfscores/sync/{archivedCircuitId}", null);
        Assert.Equal(HttpStatusCode.OK, syncArchivedResponse.StatusCode);

        var rankingResponse = await _client.GetAsync($"/v1/rankings?categoryId={categoryId}&year=2026&page=1&limit=10");
        Assert.Equal(HttpStatusCode.OK, rankingResponse.StatusCode);

        using var rankingJson = JsonDocument.Parse(await rankingResponse.Content.ReadAsStringAsync());
        var data = rankingJson.RootElement.GetProperty("data");
        Assert.Equal(1, data.GetArrayLength());
        Assert.Equal("Lucia Current", data[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task GetRanking_ShouldRespectCategoryBestResultsCount()
    {
        var circuitId = await CreateCircuitAsync("Ranking Best Results", "ALAS-RANK-BEST-26", "Activo");
        var categoryId = await CreateCategoryAsync(bestResultsCount: 1);
        var firstEventId = await CreateEventAsync(circuitId, "Best Event 1", "BEST-EV-1-26");
        var secondEventId = await CreateEventAsync(circuitId, "Best Event 2", "BEST-EV-2-26");

        await AssignCategoryToEventAsync(firstEventId, categoryId);
        await AssignCategoryToEventAsync(secondEventId, categoryId);

        var competitorId = await CreateCompetitorAsync("Julia", "Top", "Peru");
        var runnerUpId = await CreateCompetitorAsync("Laura", "Ride", "Chile");

        await CreatePaidInscriptionAsync(competitorId, firstEventId, categoryId);
        await CreatePaidInscriptionAsync(competitorId, secondEventId, categoryId);
        await CreatePaidInscriptionAsync(runnerUpId, firstEventId, categoryId);

        await UpsertEventResultsAsync(
            firstEventId,
            categoryId,
            (competitorId, "2", 250),
            (runnerUpId, "1", 300));
        await UpsertEventResultsAsync(
            secondEventId,
            categoryId,
            (competitorId, "1", 500));

        var syncResponse = await _client.PostAsync($"/v1/surfscores/sync/{circuitId}", null);
        Assert.Equal(HttpStatusCode.OK, syncResponse.StatusCode);

        var rankingResponse = await _client.GetAsync($"/v1/rankings?categoryId={categoryId}&year=2026&page=1&limit=10");
        Assert.Equal(HttpStatusCode.OK, rankingResponse.StatusCode);

        using var rankingJson = JsonDocument.Parse(await rankingResponse.Content.ReadAsStringAsync());
        var winner = rankingJson.RootElement.GetProperty("data")[0];
        Assert.Equal("Julia Top", winner.GetProperty("name").GetString());
        Assert.Equal(500, winner.GetProperty("points").GetInt32());
    }

    private Task<string> CreateCircuitAsync()
        => CreateCircuitAsync("Ranking Tour", "ALAS-RANK-26", "Activo");

    private async Task<string> CreateCircuitAsync(string nombre, string surfScoresCode, string estado)
    {
        var response = await _client.PostAsJsonAsync("/v1/circuits", new
        {
            nombre,
            temporada = 2026,
            descripcion = "Circuito ranking",
            region = "Latinoamérica",
            modalidad = "Shortboard",
            estado,
            surfScoresCode
        });

        return await ReadIdAsync(response);
    }

    private async Task<string> CreateCategoryAsync(int? bestResultsCount = null)
    {
        var response = await _client.PostAsJsonAsync("/v1/categories", new
        {
            nombre = "Open Mujeres",
            descripcion = "Categoria ranking",
            gender = "Femenino",
            ageRestriction = false,
            minAge = (int?)null,
            maxAge = (int?)null,
            status = "Activo",
            bestResultsCount
        });

        return await ReadIdAsync(response);
    }

    private Task<string> CreateEventAsync(string circuitId)
        => CreateEventAsync(circuitId, "Ranking Pro", "RANKING-PRO-26");

    private async Task<string> CreateEventAsync(string circuitId, string nombre, string surfScoresCode)
    {
        var response = await _client.PostAsJsonAsync("/v1/events", new
        {
            nombre,
            circuitId,
            fechaInicio = "2026-08-10",
            fechaFin = "2026-08-12",
            pais = "Perú",
            ciudad = "Lima",
            playa = "Punta Rocas",
            stars = 4,
            capacidadMaxima = 64,
            prizeAmountUsd = 10000,
            surfScoresCode,
            accessType = "Abierto",
            estado = "Activo"
        });

        return await ReadIdAsync(response);
    }

    private async Task AssignCategoryToEventAsync(string eventId, string categoryId)
    {
        var response = await _client.PutAsJsonAsync($"/v1/events/{eventId}/categories", new
        {
            useCircuitTariffs = false,
            categories = new[]
            {
                new
                {
                    categoryId,
                    customTariffUsd = 150,
                    capacidad = 32
                }
            }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<string> CreateCompetitorAsync(string nombre, string apellido, string pais)
    {
        var response = await _client.PostAsJsonAsync("/v1/competitors", new
        {
            nombre,
            apellido,
            email = $"{nombre.ToLowerInvariant()}.{apellido.ToLowerInvariant()}@test.com",
            telefono = "3000000000",
            pais,
            fechaNacimiento = "2000-01-01",
            genero = "Femenino",
            club = "ALAS",
            federacion = "Federacion",
            patrocinadores = "Marca",
            postura = "Regular",
            tallaCamiseta = "M",
            numeroCamiseta = "10"
        });

        return await ReadIdAsync(response);
    }

    private async Task CreatePaidInscriptionAsync(string competitorId, string eventId, string categoryId)
    {
        var inscriptionResponse = await _client.PostAsJsonAsync("/v1/inscriptions", new
        {
            competitorId,
            eventId,
            categoryId,
            shirtNumber = "11",
            paymentMethod = "paypal",
            reglamento = true,
            riesgosAceptados = true,
            usoImagenAceptado = true
        });

        var inscriptionId = await ReadIdAsync(inscriptionResponse);

        var paymentResponse = await _client.PostAsJsonAsync("/v1/payments", new
        {
            inscriptionId,
            method = "paypal",
            amountUsd = 150.0,
            transactionId = $"TX-{Guid.NewGuid():N}"
        });

        Assert.Equal(HttpStatusCode.Created, paymentResponse.StatusCode);
    }

    private async Task UpsertEventResultsAsync(string eventId, string categoryId, params (string CompetitorId, string Place, int LigaPoints)[] results)
    {
        var response = await _client.PostAsJsonAsync($"/v1/events/{eventId}/results", new
        {
            categoryId,
            results = results.Select(result => new
            {
                competitorId = result.CompetitorId,
                place = result.Place,
                ligaPoints = result.LigaPoints,
                prizeUsd = (decimal?)null,
                heatOla1 = (decimal?)null,
                heatOla2 = (decimal?)null
            }).ToArray()
        });

        var body = await response.Content.ReadAsStringAsync();
        if (response.StatusCode != HttpStatusCode.Created)
        {
            throw new InvalidOperationException(
                $"Expected 201 Created but received {(int)response.StatusCode} {response.StatusCode}. Body: {body}");
        }
    }

    private static async Task<string> ReadIdAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        if (response.StatusCode != HttpStatusCode.Created)
        {
            throw new InvalidOperationException(
                $"Expected 201 Created but received {(int)response.StatusCode} {response.StatusCode}. Body: {body}");
        }

        using var json = JsonDocument.Parse(body);
        return json.RootElement.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("Response without id.");
    }
}
