using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AlasApp.Application.AdminSettings;
using AlasApp.Domain.Entities;
using AlasApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AlasApp.Api.Tests;

public sealed class InscriptionsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public InscriptionsEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task InscriptionCrudFlow_Works_EndToEnd()
    {
        await TestAdminAuthHelper.AuthenticateAsAdminAsync(_client, _factory.Services);

        var circuitId = await CreateCircuitAsync();
        var eventId = await CreateEventAsync(circuitId);
        var categoryId = await CreateCategoryAsync("Open Mixto");
        var deletableCategoryId = await CreateCategoryAsync("Junior Mixto");
        var competitor = await RegisterAndLoginCompetitorAsync();
        var competitorId = competitor.CompetitorId;

        var assignCategoryResponse = await _client.PutAsJsonAsync($"/v1/events/{eventId}/categories", new
        {
            useCircuitTariffs = false,
            categories = new[]
            {
                new
                {
                    categoryId,
                    customTariffUsd = 80,
                    capacidad = 2
                },
                new
                {
                    categoryId = deletableCategoryId,
                    customTariffUsd = 55,
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
            reglamento = true,
            riesgosAceptados = true,
            usoImagenAceptado = true
        });

        Assert.Equal(HttpStatusCode.Created, createInscriptionResponse.StatusCode);

        var created = await ReadJsonAsync(createInscriptionResponse);
        var inscriptionId = created.RootElement.GetProperty("id").GetString();
        Assert.Equal("paypal", created.RootElement.GetProperty("paymentMethod").GetString());
        Assert.Equal("Pendiente", created.RootElement.GetProperty("estadoAdmin").GetString());
        Assert.Equal(80m, created.RootElement.GetProperty("baseAmountUsd").GetDecimal());
        Assert.False(created.RootElement.TryGetProperty("administrativeFeeUsd", out _));
        Assert.Equal(80m, created.RootElement.GetProperty("montoUsd").GetDecimal());
        Assert.True(created.RootElement.GetProperty("reglamentoAceptado").GetBoolean());
        Assert.True(created.RootElement.GetProperty("riesgosAceptados").GetBoolean());
        Assert.True(created.RootElement.GetProperty("usoImagenAceptado").GetBoolean());

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
            reglamento = true,
            riesgosAceptados = true,
            usoImagenAceptado = true
        });

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", competitor.Token);

        var deletePaidResponse = await _client.DeleteAsync($"/v1/inscriptions/{inscriptionId}");
        Assert.Equal(HttpStatusCode.Conflict, deletePaidResponse.StatusCode);

        var createPendingDeleteResponse = await _client.PostAsJsonAsync("/v1/inscriptions", new
        {
            competitorId,
            eventId,
            categoryId = deletableCategoryId,
            shirtNumber = "#55",
            paymentMethod = "paypal",
            reglamento = true,
            riesgosAceptados = true,
            usoImagenAceptado = true
        });

        Assert.Equal(HttpStatusCode.Created, createPendingDeleteResponse.StatusCode);

        var pendingDeleteBody = await ReadJsonAsync(createPendingDeleteResponse);
        var pendingDeleteId = pendingDeleteBody.RootElement.GetProperty("id").GetString();

        var deleteResponse = await _client.DeleteAsync($"/v1/inscriptions/{pendingDeleteId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getAfterDeleteResponse = await _client.GetAsync($"/v1/inscriptions/{pendingDeleteId}");
        Assert.Equal(HttpStatusCode.NotFound, getAfterDeleteResponse.StatusCode);
    }

    [Fact]
    public async Task CreateInscription_ShouldIncludeAdministrativeFeeBreakdown_WhenConfigured()
    {
        await TestAdminAuthHelper.AuthenticateAsAdminAsync(_client, _factory.Services);
        await SeedAdministrativeFeeAsync(15m);

        var circuitId = await CreateCircuitAsync();
        var eventId = await CreateEventAsync(circuitId);
        var categoryId = await CreateCategoryAsync("Open Fee");
        var competitor = await RegisterAndLoginCompetitorAsync();

        var assignCategoryResponse = await _client.PutAsJsonAsync($"/v1/events/{eventId}/categories", new
        {
            useCircuitTariffs = false,
            categories = new[]
            {
                new
                {
                    categoryId,
                    customTariffUsd = 80,
                    capacidad = 2
                }
            }
        });

        Assert.Equal(HttpStatusCode.OK, assignCategoryResponse.StatusCode);

        var createInscriptionResponse = await _client.PostAsJsonAsync("/v1/inscriptions", new
        {
            competitorId = competitor.CompetitorId,
            eventId,
            categoryId,
            shirtNumber = "#88",
            paymentMethod = "paypal",
            reglamento = true,
            riesgosAceptados = true,
            usoImagenAceptado = true
        });

        Assert.Equal(HttpStatusCode.Created, createInscriptionResponse.StatusCode);

        var created = await ReadJsonAsync(createInscriptionResponse);
        Assert.Equal(80m, created.RootElement.GetProperty("baseAmountUsd").GetDecimal());
        Assert.Equal(15m, created.RootElement.GetProperty("administrativeFeeUsd").GetDecimal());
        Assert.Equal(95m, created.RootElement.GetProperty("montoUsd").GetDecimal());
    }

    [Fact]
    public async Task CreateInscription_ShouldRejectWhenRequiredConsentsAreMissing()
    {
        await TestAdminAuthHelper.AuthenticateAsAdminAsync(_client, _factory.Services);

        var circuitId = await CreateCircuitAsync();
        var eventId = await CreateEventAsync(circuitId);
        var categoryId = await CreateCategoryAsync("Open Consent");
        var competitor = await RegisterAndLoginCompetitorAsync();

        var assignCategoryResponse = await _client.PutAsJsonAsync($"/v1/events/{eventId}/categories", new
        {
            useCircuitTariffs = false,
            categories = new[]
            {
                new
                {
                    categoryId,
                    customTariffUsd = 80,
                    capacidad = 2
                }
            }
        });

        Assert.Equal(HttpStatusCode.OK, assignCategoryResponse.StatusCode);

        var createInscriptionResponse = await _client.PostAsJsonAsync("/v1/inscriptions", new
        {
            competitorId = competitor.CompetitorId,
            eventId,
            categoryId,
            shirtNumber = "#18",
            paymentMethod = "paypal",
            reglamento = true,
            riesgosAceptados = false,
            usoImagenAceptado = false
        });

        Assert.Equal(HttpStatusCode.BadRequest, createInscriptionResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteInscription_ShouldRejectWhenCompetitorDoesNotOwnIt()
    {
        await TestAdminAuthHelper.AuthenticateAsAdminAsync(_client, _factory.Services);

        var circuitId = await CreateCircuitAsync();
        var eventId = await CreateEventAsync(circuitId);
        var categoryId = await CreateCategoryAsync("Open Hombres");
        var owner = await RegisterAndLoginCompetitorAsync();
        var other = await RegisterAndLoginCompetitorAsync();

        var assignCategoryResponse = await _client.PutAsJsonAsync($"/v1/events/{eventId}/categories", new
        {
            useCircuitTariffs = false,
            categories = new[]
            {
                new
                {
                    categoryId,
                    customTariffUsd = 80,
                    capacidad = 2
                }
            }
        });

        Assert.Equal(HttpStatusCode.OK, assignCategoryResponse.StatusCode);

        var createInscriptionResponse = await _client.PostAsJsonAsync("/v1/inscriptions", new
        {
            competitorId = owner.CompetitorId,
            eventId,
            categoryId,
            shirtNumber = "#23",
            paymentMethod = "paypal",
            reglamento = true,
            riesgosAceptados = true,
            usoImagenAceptado = true
        });

        Assert.Equal(HttpStatusCode.Created, createInscriptionResponse.StatusCode);

        var created = await ReadJsonAsync(createInscriptionResponse);
        var inscriptionId = created.RootElement.GetProperty("id").GetString();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", other.Token);

        var deleteResponse = await _client.DeleteAsync($"/v1/inscriptions/{inscriptionId}");
        Assert.Equal(HttpStatusCode.Unauthorized, deleteResponse.StatusCode);
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

    private async Task<string> CreateCategoryAsync(string nombre)
    {
        var response = await _client.PostAsJsonAsync("/v1/categories", new
        {
            nombre,
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

    private async Task<(string CompetitorId, string Token)> RegisterAndLoginCompetitorAsync()
    {
        var email = $"competidor-{Guid.NewGuid():N}@test.com";

        var registerResponse = await _client.PostAsJsonAsync("/v1/auth/register", new
        {
            email,
            password = "Password1",
            nombre = "Carlos",
            apellido = "Diaz",
            tipo = "competidor",
            pais = "Perú",
            idiomaPreferido = "Español",
            newsletter = true,
            terminos = true,
            reglamento = true,
            fechaNacimiento = "1997-02-05",
            genero = "Masculino",
            telefono = "+51 900 123 456",
            club = "Club Ola",
            postura = "Regular",
            tallaCamiseta = "M",
            patrocinadores = "Marca E",
            federacion = "FENTA"
        });

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var loginResponse = await _client.PostAsJsonAsync("/v1/auth/login", new
        {
            email,
            password = "Password1",
            rememberMe = false
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginJson = JObject.Parse(await loginResponse.Content.ReadAsStringAsync());
        return (
            loginJson["user"]?["competitorId"]?.Value<string>()!,
            loginJson["accessToken"]?.Value<string>()!);
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(content);
    }

    private async Task SeedAdministrativeFeeAsync(decimal amountUsd)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AlasAppDbContext>();

        var settings = AdminSettingsDefaults.Create();
        settings = settings with
        {
            General = settings.General with { AdministrativeFeeUsd = amountUsd }
        };

        var json = AdminSettingsSerializer.Serialize(settings);
        var existing = await dbContext.SystemSettings.FirstOrDefaultAsync(x => x.Key == AdminSettingsDefaults.SettingsKey);

        if (existing is null)
        {
            dbContext.SystemSettings.Add(SystemSetting.Create(AdminSettingsDefaults.SettingsKey, json, DateTimeOffset.UtcNow));
        }
        else
        {
            existing.Update(json, DateTimeOffset.UtcNow);
        }

        await dbContext.SaveChangesAsync();
    }
}
