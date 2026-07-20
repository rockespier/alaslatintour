using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace AlasApp.Api.Tests;

public sealed class PaymentsAndBeachTokensEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PaymentsAndBeachTokensEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PaymentsAndBeachTokensFlow_Works_EndToEnd()
    {
        await TestAdminAuthHelper.AuthenticateAsAdminAsync(_client, _factory.Services);

        var circuitId = await CreateCircuitAsync();
        var eventId = await CreateEventAsync(circuitId);
        var categoryId = await CreateCategoryAsync();

        var assignCategoryResponse = await _client.PutAsJsonAsync($"/v1/events/{eventId}/categories", new
        {
            useCircuitTariffs = false,
            categories = new[]
            {
                new
                {
                    categoryId,
                    customTariffUsd = 95,
                    capacidad = 5
                }
            }
        });

        Assert.Equal(HttpStatusCode.OK, assignCategoryResponse.StatusCode);

        var competitorBeachId = await CreateCompetitorAsync("Laura", "Mendez", "laura.mendez@example.com");
        var beachInscriptionId = await CreateInscriptionAsync(competitorBeachId, eventId, categoryId, "beach");

        var requestTokenResponse = await _client.PostAsJsonAsync("/v1/payments/beach/request", new
        {
            inscriptionId = beachInscriptionId
        });

        Assert.Equal(HttpStatusCode.Created, requestTokenResponse.StatusCode);

        var requestTokenBody = await ReadJsonAsync(requestTokenResponse);
        var tokenId = requestTokenBody.RootElement.GetProperty("requestId").GetString();
        Assert.Equal("pending", requestTokenBody.RootElement.GetProperty("status").GetString());

        var duplicateRequestResponse = await _client.PostAsJsonAsync("/v1/payments/beach/request", new
        {
            inscriptionId = beachInscriptionId
        });

        Assert.Equal(HttpStatusCode.Conflict, duplicateRequestResponse.StatusCode);

        var listTokensResponse = await _client.GetAsync("/v1/payments/beach/tokens");
        Assert.Equal(HttpStatusCode.OK, listTokensResponse.StatusCode);

        var listTokensBody = await ReadJsonAsync(listTokensResponse);
        Assert.True(listTokensBody.RootElement.GetProperty("pendingRequests").GetArrayLength() >= 1);

        var approveResponse = await _client.PostAsync($"/v1/payments/beach/tokens/{tokenId}/approve", null);
        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);

        var approveBody = await ReadJsonAsync(approveResponse);
        var tokenCode = approveBody.RootElement.GetProperty("tokenCode").GetString();
        Assert.False(string.IsNullOrWhiteSpace(tokenCode));

        var redeemResponse = await _client.PostAsJsonAsync("/v1/payments/beach/redeem", new
        {
            inscriptionId = beachInscriptionId,
            tokenCode
        });

        Assert.Equal(HttpStatusCode.OK, redeemResponse.StatusCode);

        var redeemBody = await ReadJsonAsync(redeemResponse);
        Assert.Equal("success", redeemBody.RootElement.GetProperty("status").GetString());
        Assert.Equal("pendiente", redeemBody.RootElement.GetProperty("financialStatus").GetString());

        var pendingPaymentsResponse = await _client.GetAsync("/v1/payments?method=Beach&status=Pendiente");
        Assert.Equal(HttpStatusCode.OK, pendingPaymentsResponse.StatusCode);

        var pendingPaymentsBody = await ReadJsonAsync(pendingPaymentsResponse);
        Assert.Equal(1, pendingPaymentsBody.RootElement.GetProperty("data").GetArrayLength());
        var beachPaymentId = pendingPaymentsBody.RootElement.GetProperty("data")[0].GetProperty("id").GetString();

        var updateBeachPaymentResponse = await _client.PutAsJsonAsync($"/v1/payments/{beachPaymentId}", new
        {
            status = "Confirmado",
            notes = "Validado en playa"
        });

        Assert.Equal(HttpStatusCode.OK, updateBeachPaymentResponse.StatusCode);

        var updatedBeachPaymentBody = await ReadJsonAsync(updateBeachPaymentResponse);
        Assert.Equal("Confirmado", updatedBeachPaymentBody.RootElement.GetProperty("estado").GetString());

        var getBeachPaymentResponse = await _client.GetAsync($"/v1/payments/{beachPaymentId}");
        Assert.Equal(HttpStatusCode.OK, getBeachPaymentResponse.StatusCode);

        var competitorPaypalId = await CreateCompetitorAsync("Mario", "Lopez", "mario.lopez@example.com");
        var paypalInscriptionId = await CreateInscriptionAsync(competitorPaypalId, eventId, categoryId, "paypal");

        var createPaypalPaymentResponse = await _client.PostAsJsonAsync("/v1/payments", new
        {
            inscriptionId = paypalInscriptionId,
            method = "paypal",
            amountUsd = 95,
            transactionId = "PP-9X8C7B2A"
        });

        Assert.Equal(HttpStatusCode.Created, createPaypalPaymentResponse.StatusCode);

        var createPaypalPaymentBody = await ReadJsonAsync(createPaypalPaymentResponse);
        Assert.Equal("Confirmado", createPaypalPaymentBody.RootElement.GetProperty("estado").GetString());

        var competitorRejectedId = await CreateCompetitorAsync("Nora", "Perez", "nora.perez@example.com");
        var rejectedInscriptionId = await CreateInscriptionAsync(competitorRejectedId, eventId, categoryId, "beach");

        var rejectedRequestResponse = await _client.PostAsJsonAsync("/v1/payments/beach/request", new
        {
            inscriptionId = rejectedInscriptionId
        });

        var rejectedRequestBody = await ReadJsonAsync(rejectedRequestResponse);
        var rejectedTokenId = rejectedRequestBody.RootElement.GetProperty("requestId").GetString();

        var rejectResponse = await _client.PostAsJsonAsync($"/v1/payments/beach/tokens/{rejectedTokenId}/reject", new
        {
            reason = "Documentacion incompleta"
        });

        Assert.Equal(HttpStatusCode.OK, rejectResponse.StatusCode);

        var rejectedListResponse = await _client.GetAsync("/v1/payments/beach/tokens?status=Rechazado");
        Assert.Equal(HttpStatusCode.OK, rejectedListResponse.StatusCode);

        var kpisResponse = await _client.GetAsync("/v1/payments/kpis");
        Assert.Equal(HttpStatusCode.OK, kpisResponse.StatusCode);

        var kpisBody = await ReadJsonAsync(kpisResponse);
        Assert.True(kpisBody.RootElement.GetProperty("totalRecaudadoMes").GetDouble() >= 190d);
        Assert.Equal(1, kpisBody.RootElement.GetProperty("pagoPaypalConfirmados").GetProperty("count").GetInt32());
    }

    private async Task<string> CreateCircuitAsync()
    {
        var response = await _client.PostAsJsonAsync("/v1/circuits", new
        {
            nombre = "Circuito Payments",
            temporada = 2026,
            descripcion = "Circuito test payments",
            region = "Latinoamérica",
            modalidad = "Shortboard",
            estado = "Activo",
            surfScoresCode = "PAY-2026"
        });

        var body = await ReadJsonAsync(response);
        return body.RootElement.GetProperty("id").GetString()!;
    }

    private async Task<string> CreateEventAsync(string circuitId)
    {
        var response = await _client.PostAsJsonAsync("/v1/events", new
        {
            nombre = "Evento Payments",
            circuitId,
            fechaInicio = "2026-10-02",
            fechaFin = "2026-10-04",
            pais = "Perú",
            ciudad = "Lima",
            playa = "Punta Rocas",
            stars = 4,
            capacidadMaxima = 5,
            prizeAmountUsd = 20000,
            surfScoresCode = "EV-PAY-2026",
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
            nombre = "Open Payments",
            descripcion = "Categoria para payments",
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

    private async Task<string> CreateCompetitorAsync(string nombre, string apellido, string email)
    {
        var response = await _client.PostAsJsonAsync("/v1/competitors", new
        {
            nombre,
            apellido,
            email,
            fechaNacimiento = "1998-03-08",
            genero = "Femenino",
            pais = "Perú",
            telefono = "+51 900 123 456",
            club = "Club Payment",
            postura = "Regular",
            tallaCamiseta = "M",
            numeroCamiseta = "#55",
            patrocinadores = "Marca P",
            federacion = "FENTA"
        });

        var body = await ReadJsonAsync(response);
        var competitorId = body.RootElement.GetProperty("id").GetString()!;
        await TestCompetitorLicense.ActivateAsync(_factory.Services, competitorId);
        return competitorId;
    }

    private async Task<string> CreateInscriptionAsync(string competitorId, string eventId, string categoryId, string paymentMethod)
    {
        var response = await _client.PostAsJsonAsync("/v1/inscriptions", new
        {
            competitorId,
            eventId,
            categoryId,
            shirtNumber = "#77",
            paymentMethod,
            reglamento = true,
            riesgosAceptados = true,
            usoImagenAceptado = true
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
