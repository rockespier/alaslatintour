using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace AlasApp.Api.Tests;

public sealed class InscriptionsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public InscriptionsEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task InscriptionCrudFlow_Works_EndToEnd()
    {
        var circuitId = await CreateCircuitAsync();
        var eventId = await CreateEventAsync(circuitId);
        var categoryId = await CreateCategoryAsync();
        var competitorId = await CreateCompetitorAsync();

        var assignCategoryResponse = await _client.PutAsJsonAsync($"/v1/events/{eventId}/categories", new
        {
            useCircuitTariffs = false,
            categories = new[]
            {
                new
                {
                    categoryId,
                    customTariffUsd = 80,
                    customTariffCop = 320000,
                    capacidad = 2
                }
            }
        });

        Assert.Equal(HttpStatusCode.OK, assignCategoryResponse.StatusCode);

        var createInscriptionResponse = await _client.PostAsJsonAsync("/v1/inscriptions", new
        {
            competitorId,
            eventId,
            categoryId,
            shirtNumber = "#99",
            paymentMethod = "paypal",
            reglamento = true
        });

        Assert.Equal(HttpStatusCode.Created, createInscriptionResponse.StatusCode);

        var created = await ReadJsonAsync(createInscriptionResponse);
        var inscriptionId = created.RootElement.GetProperty("id").GetString();
        Assert.Equal("paypal", created.RootElement.GetProperty("paymentMethod").GetString());
        Assert.Equal("Pendiente", created.RootElement.GetProperty("estadoAdmin").GetString());

        var getResponse = await _client.GetAsync($"/v1/inscriptions/{inscriptionId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var listResponse = await _client.GetAsync($"/v1/inscriptions?eventId={eventId}&categoryId={categoryId}&status=Pendiente");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var listBody = await ReadJsonAsync(listResponse);
        Assert.Equal(1, listBody.RootElement.GetProperty("data").GetArrayLength());

        var eventListResponse = await _client.GetAsync($"/v1/events/{eventId}/inscriptions?categoryId={categoryId}&status=Pendiente");
        Assert.Equal(HttpStatusCode.OK, eventListResponse.StatusCode);

        var competitorListResponse = await _client.GetAsync($"/v1/competitors/{competitorId}/inscriptions?status=pendiente");
        Assert.Equal(HttpStatusCode.OK, competitorListResponse.StatusCode);

        var competitorListBody = await ReadJsonAsync(competitorListResponse);
        Assert.Equal(1, competitorListBody.RootElement.GetProperty("data").GetArrayLength());

        var calendarResponse = await _client.GetAsync($"/v1/competitors/{competitorId}/calendar");
        Assert.Equal(HttpStatusCode.OK, calendarResponse.StatusCode);

        var calendarBody = await ReadJsonAsync(calendarResponse);
        Assert.Equal(1, calendarBody.RootElement.GetProperty("data").GetArrayLength());

        var updateResponse = await _client.PutAsJsonAsync($"/v1/inscriptions/{inscriptionId}", new
        {
            shirtNumber = "#100",
            estadoAdmin = "Pagado",
            notes = "Pago confirmado"
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updated = await ReadJsonAsync(updateResponse);
        Assert.Equal("Pagado", updated.RootElement.GetProperty("estadoAdmin").GetString());
        Assert.Equal("confirmado", updated.RootElement.GetProperty("estadoCompetidor").GetString());

        var duplicateResponse = await _client.PostAsJsonAsync("/v1/inscriptions", new
        {
            competitorId,
            eventId,
            categoryId,
            shirtNumber = "#77",
            paymentMethod = "paypal",
            reglamento = true
        });

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);

        var deleteResponse = await _client.DeleteAsync($"/v1/inscriptions/{inscriptionId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getAfterDeleteResponse = await _client.GetAsync($"/v1/inscriptions/{inscriptionId}");
        Assert.Equal(HttpStatusCode.NotFound, getAfterDeleteResponse.StatusCode);
    }

    private async Task<string> CreateCircuitAsync()
    {
        var response = await _client.PostAsJsonAsync("/v1/circuits", new
        {
            nombre = "Circuito Inscriptions",
            temporada = 2026,
            descripcion = "Circuito test inscriptions",
            region = "Latinoamérica",
            modalidad = "Shortboard",
            estado = "Activo",
            surfScoresCode = "INS-2026"
        });

        var body = await ReadJsonAsync(response);
        return body.RootElement.GetProperty("id").GetString()!;
    }

    private async Task<string> CreateEventAsync(string circuitId)
    {
        var response = await _client.PostAsJsonAsync("/v1/events", new
        {
            nombre = "Evento Inscriptions",
            circuitId,
            fechaInicio = "2026-09-10",
            fechaFin = "2026-09-12",
            pais = "Perú",
            ciudad = "Lima",
            playa = "Punta Rocas",
            stars = 4,
            capacidadMaxima = 120,
            prizeAmountUsd = 15000,
            surfScoresCode = "EV-INS-2026",
            accessType = "Abierto",
            estado = "Activo"
        });

        var body = await ReadJsonAsync(response);
        return body.RootElement.GetProperty("id").GetString()!;
    }

    private async Task<string> CreateCategoryAsync()
    {
        var response = await _client.PostAsJsonAsync("/v1/categories", new
        {
            nombre = "Open Mixto",
            descripcion = "Categoria para inscriptions",
            gender = "Ambos",
            ageRestriction = false,
            minAge = (int?)null,
            maxAge = (int?)null,
            successorCategoryId = (string?)null,
            status = "Activo"
        });

        var body = await ReadJsonAsync(response);
        return body.RootElement.GetProperty("id").GetString()!;
    }

    private async Task<string> CreateCompetitorAsync()
    {
        var response = await _client.PostAsJsonAsync("/v1/competitors", new
        {
            nombre = "Carlos",
            apellido = "Diaz",
            email = "carlos.diaz@example.com",
            fechaNacimiento = "1997-02-05",
            genero = "Masculino",
            pais = "Perú",
            telefono = "+51 900 123 456",
            club = "Club Ola",
            postura = "Regular",
            tallaCamiseta = "M",
            numeroCamiseta = "#44",
            patrocinadores = "Marca E",
            federacion = "FENTA"
        });

        var body = await ReadJsonAsync(response);
        return body.RootElement.GetProperty("id").GetString()!;
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(content);
    }
}
