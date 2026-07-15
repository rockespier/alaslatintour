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

    private async Task<string> CreateCircuitAsync()
    {
        var response = await _client.PostAsJsonAsync("/v1/circuits", new
        {
            nombre = "Ranking Tour",
            temporada = 2026,
            descripcion = "Circuito ranking",
            region = "Latinoamérica",
            modalidad = "Shortboard",
            estado = "Activo",
            surfScoresCode = "ALAS-RANK-26"
        });

        return await ReadIdAsync(response);
    }

    private async Task<string> CreateCategoryAsync()
    {
        var response = await _client.PostAsJsonAsync("/v1/categories", new
        {
            nombre = "Open Mujeres",
            descripcion = "Categoria ranking",
            gender = "Femenino",
            ageRestriction = false,
            minAge = (int?)null,
            maxAge = (int?)null,
            status = "Activo"
        });

        return await ReadIdAsync(response);
    }

    private async Task<string> CreateEventAsync(string circuitId)
    {
        var response = await _client.PostAsJsonAsync("/v1/events", new
        {
            nombre = "Ranking Pro",
            circuitId,
            fechaInicio = "2026-08-10",
            fechaFin = "2026-08-12",
            pais = "Perú",
            ciudad = "Lima",
            playa = "Punta Rocas",
            stars = 4,
            capacidadMaxima = 64,
            prizeAmountUsd = 10000,
            surfScoresCode = "RANKING-PRO-26",
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
            reglamento = true
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
