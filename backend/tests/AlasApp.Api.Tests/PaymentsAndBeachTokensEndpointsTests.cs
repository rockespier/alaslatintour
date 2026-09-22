using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ClosedXML.Excel;
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

    [Fact]
    public async Task MembershipPayments_ShouldListConfirmedMembershipFeeOnly()
    {
        await TestAdminAuthHelper.AuthenticateAsAdminAsync(_client, _factory.Services);

        var circuitId = await CreateCircuitAsync();
        var eventId = await CreateEventAsync(circuitId);

        var categoryResponse = await _client.PostAsJsonAsync("/v1/categories", new
        {
            nombre = "Open Membership",
            descripcion = "Categoria con membresia por evento",
            gender = "Ambos",
            ageRestriction = false,
            minAge = (int?)null,
            maxAge = (int?)null,
            successorCategoryId = (string?)null,
            status = "Activo",
            membresiaAnualUsd = 0,
            membresiaPorEventoUsd = 30
        });
        var categoryBody = await ReadJsonAsync(categoryResponse);
        var categoryId = categoryBody.RootElement.GetProperty("id").GetString()!;

        var assignResponse = await _client.PutAsJsonAsync($"/v1/events/{eventId}/categories", new
        {
            useCircuitTariffs = false,
            categories = new[] { new { categoryId, customTariffUsd = 95, capacidad = 5 } }
        });
        Assert.Equal(HttpStatusCode.OK, assignResponse.StatusCode);

        var competitorId = await CreateCompetitorAsync("Elena", "Vargas", "elena.vargas@example.com");

        var bulkResponse = await _client.PostAsJsonAsync("/v1/inscriptions/bulk", new
        {
            competitorId,
            eventId,
            categoryIds = new[] { categoryId },
            paymentMethod = "paypal",
            membershipPlan = "PorEvento",
            reglamento = true,
            riesgosAceptados = true,
            usoImagenAceptado = true
        });
        Assert.Equal(HttpStatusCode.Created, bulkResponse.StatusCode);
        var bulkBody = await ReadJsonAsync(bulkResponse);
        var primaryInscriptionId = bulkBody.RootElement.GetProperty("primaryInscriptionId").GetString();
        var totalUsd = bulkBody.RootElement.GetProperty("totalMontoUsd").GetDecimal();
        Assert.Equal(125m, totalUsd);

        var paymentResponse = await _client.PostAsJsonAsync("/v1/payments", new
        {
            inscriptionId = primaryInscriptionId,
            method = "paypal",
            amountUsd = totalUsd,
            transactionId = $"PP-MEMBER-{Guid.NewGuid():N}"[..20]
        });
        Assert.Equal(HttpStatusCode.Created, paymentResponse.StatusCode);

        var membershipsResponse = await _client.GetAsync("/v1/payments/memberships");
        var membershipsRaw = await membershipsResponse.Content.ReadAsStringAsync();
        Assert.True(membershipsResponse.StatusCode == HttpStatusCode.OK, membershipsRaw);

        var membershipsBody = await ReadJsonAsync(membershipsResponse);
        var rows = membershipsBody.RootElement.GetProperty("data").EnumerateArray().ToList();
        var row = rows.First(x => x.GetProperty("competitorName").GetString() == "Elena Vargas");

        Assert.Equal("PorEvento", row.GetProperty("membershipPlan").GetString());
        Assert.Equal("Evento Payments", row.GetProperty("eventName").GetString());
        Assert.Equal("Circuito Payments", row.GetProperty("circuitName").GetString());
        Assert.Equal("Paypal", row.GetProperty("method").GetString());
        Assert.Equal(30m, row.GetProperty("amountUsd").GetDecimal());
    }

    [Fact]
    public async Task MembershipPaymentsImport_ShouldAttachMembershipToExistingInscriptionAndCreatePayment()
    {
        await TestAdminAuthHelper.AuthenticateAsAdminAsync(_client, _factory.Services);

        var circuitId = await CreateCircuitAsync();
        var eventId = await CreateEventAsync(circuitId);
        var categoryId = await CreateCategoryAsync();

        var assignResponse = await _client.PutAsJsonAsync($"/v1/events/{eventId}/categories", new
        {
            useCircuitTariffs = false,
            categories = new[] { new { categoryId, customTariffUsd = 95, capacidad = 5 } }
        });
        Assert.Equal(HttpStatusCode.OK, assignResponse.StatusCode);

        var competitorId = await CreateCompetitorAsync("Bruna", "Silva", $"bruna.silva-{Guid.NewGuid():N}@example.com");

        // Inscripcion ya existente, SIN membresia ni pago (simula carga masiva historica por SQL).
        var inscriptionId = await CreateInscriptionAsync(competitorId, eventId, categoryId, "paypal");

        var templateResponse = await _client.GetAsync($"/v1/events/{eventId}/membership-payments/template");
        Assert.Equal(HttpStatusCode.OK, templateResponse.StatusCode);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", templateResponse.Content.Headers.ContentType?.MediaType);

        using var importWorkbook = new XLWorkbook();
        var worksheet = importWorkbook.Worksheets.Add("Membresias");
        WriteRow(worksheet, 1, "CompetidorId", "SurfScoresCode", "Email", "InscripcionId", "TipoMembresia", "FechaPago", "MetodoPago", "TransaccionId", "Importe");
        WriteRow(worksheet, 2, competitorId, "", "", "", "PorEvento", "2026-01-15", "Paypal", "", "30");

        var importResponse = await _client.PostAsync(
            $"/v1/events/{eventId}/membership-payments/import",
            CreateExcelForm(importWorkbook, "membresias-import.xlsx"));
        var importBody = await importResponse.Content.ReadAsStringAsync();
        Assert.True(importResponse.StatusCode == HttpStatusCode.OK, importBody);

        var importPayload = await ReadJsonAsync(importResponse);
        Assert.Equal(1, importPayload.RootElement.GetProperty("processedRows").GetInt32());
        Assert.Equal(1, importPayload.RootElement.GetProperty("updatedCount").GetInt32());
        Assert.Equal(0, importPayload.RootElement.GetProperty("errors").GetArrayLength());

        var inscriptionResponse = await _client.GetAsync($"/v1/inscriptions/{inscriptionId}");
        var inscriptionBody = await ReadJsonAsync(inscriptionResponse);
        Assert.Equal(125d, inscriptionBody.RootElement.GetProperty("montoUsd").GetDouble());

        var membershipsResponse = await _client.GetAsync("/v1/payments/memberships");
        var membershipsBody = await ReadJsonAsync(membershipsResponse);
        var row = membershipsBody.RootElement.GetProperty("data").EnumerateArray()
            .First(x => x.GetProperty("competitorName").GetString() == "Bruna Silva");
        Assert.Equal("PorEvento", row.GetProperty("membershipPlan").GetString());
        Assert.Equal(30d, row.GetProperty("amountUsd").GetDouble());

        // Reimportar la misma fila no debe duplicar el pago (Payments.TransactionId es UNIQUE).
        using var reimportWorkbook = new XLWorkbook();
        var reimportSheet = reimportWorkbook.Worksheets.Add("Membresias");
        WriteRow(reimportSheet, 1, "CompetidorId", "SurfScoresCode", "Email", "InscripcionId", "TipoMembresia", "FechaPago", "MetodoPago", "TransaccionId", "Importe");
        WriteRow(reimportSheet, 2, competitorId, "", "", "", "PorEvento", "2026-01-15", "Paypal", "", "30");

        var reimportResponse = await _client.PostAsync(
            $"/v1/events/{eventId}/membership-payments/import",
            CreateExcelForm(reimportWorkbook, "membresias-reimport.xlsx"));
        var reimportBody = await reimportResponse.Content.ReadAsStringAsync();
        Assert.True(reimportResponse.StatusCode == HttpStatusCode.OK, reimportBody);
        var reimportPayload = JsonDocument.Parse(reimportBody);
        Assert.Equal(1, reimportPayload.RootElement.GetProperty("updatedCount").GetInt32());
        Assert.Equal(0, reimportPayload.RootElement.GetProperty("errors").GetArrayLength());
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
        for (var i = 0; i < values.Length; i++)
        {
            worksheet.Cell(rowNumber, i + 1).Value = values[i];
        }
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
