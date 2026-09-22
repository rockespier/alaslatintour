using AlasApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System.Net.Http.Json;
using Xunit;

namespace AlasApp.Api.Tests;

/// <summary>
/// Reproduce contra SQL Server real (no InMemory) el escenario reportado: inscripciones insertadas
/// por script SQL directo a la tabla Inscriptions (sin pasar por la API), para verificar si el
/// conteo "inscritosCount" de GET /v1/admin/dashboard las refleja igual que a las creadas via API.
/// </summary>
public sealed class AdminDashboardBulkSqlInscriptionTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AdminDashboardBulkSqlInscriptionTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task DashboardInscritosCount_ShouldIncludeInscriptionsInsertedDirectlyBySql()
    {
        await TestAdminAuthHelper.AuthenticateAsAdminAsync(_client, _factory.Services);

        var circuitId = await CreateCircuitAsync();
        var eventId = await CreateEventAsync(circuitId);
        var categoryId = await CreateCategoryAsync();
        await AssignCategoryAsync(eventId, categoryId);

        var webCompetitorId = await CreateCompetitorAsync("Web", "Directa", $"dash-web-{Guid.NewGuid():N}@test.com");
        await CreateInscriptionAsync(webCompetitorId, eventId, categoryId, "paypal");

        var bulkCompetitorId = await CreateCompetitorAsync("Sql", "Bulk", $"dash-bulk-{Guid.NewGuid():N}@test.com");
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AlasAppDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO Inscriptions (Id, CompetitorId, InscriptionGroupId, EventId, CategoryId, PaymentMethod,
                    BaseAmountUsd, AdministrativeFeeUsd, MembershipFeeUsd, MontoUsd, EstadoAdmin, EstadoCompetidor,
                    ReglamentoAceptado, RiesgosAceptados, UsoImagenAceptado, InscripcionAt, CreatedAtUtc, UpdatedAtUtc)
                VALUES (NEWID(), {Guid.Parse(bulkCompetitorId)}, NEWID(), {Guid.Parse(eventId)}, {Guid.Parse(categoryId)}, 'Paypal',
                    95, 0, 0, 95, 'Pagado', 'Confirmado',
                    1, 1, 1, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET())");
        }

        var dashboardResponse = await _client.GetAsync("/v1/admin/dashboard");
        var dashboardJson = JObject.Parse(await dashboardResponse.Content.ReadAsStringAsync());
        var activeEvent = dashboardJson["activeEvents"]!.First(x => x["id"]?.Value<string>() == eventId);

        Assert.Equal(2, activeEvent["inscritosCount"]?.Value<int>());
    }

    private async Task AssignCategoryAsync(string eventId, string categoryId)
    {
        var response = await _client.PutAsJsonAsync($"/v1/events/{eventId}/categories", new
        {
            useCircuitTariffs = false,
            categories = new[]
            {
                new { categoryId, customTariffUsd = 95, capacidad = 20 }
            }
        });

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<string> CreateCircuitAsync()
    {
        var response = await _client.PostAsJsonAsync("/v1/circuits", new
        {
            nombre = "Circuito Dashboard Bulk",
            temporada = 2026,
            descripcion = "Circuito dashboard bulk",
            region = "Latinoamérica",
            modalidad = "Shortboard",
            estado = "Activo",
            surfScoresCode = $"DASHB-{Guid.NewGuid():N}".Substring(0, 12)
        });

        var body = JObject.Parse(await response.Content.ReadAsStringAsync());
        return body["id"]!.Value<string>()!;
    }

    private async Task<string> CreateEventAsync(string circuitId)
    {
        var response = await _client.PostAsJsonAsync("/v1/events", new
        {
            nombre = "Evento Dashboard Bulk",
            circuitId,
            fechaInicio = "2026-10-02",
            fechaFin = "2026-10-04",
            pais = "Perú",
            ciudad = "Lima",
            playa = "Punta Rocas",
            stars = 4,
            capacidadMaxima = 20,
            prizeAmountUsd = 20000,
            surfScoresCode = $"EVB-{Guid.NewGuid():N}".Substring(0, 12),
            accessType = "Abierto",
            estado = "Completado"
        });

        var body = JObject.Parse(await response.Content.ReadAsStringAsync());
        return body["id"]!.Value<string>()!;
    }

    private async Task<string> CreateCategoryAsync()
    {
        var response = await _client.PostAsJsonAsync("/v1/categories", new
        {
            nombre = $"OpenDashB{Guid.NewGuid():N}"[..16],
            descripcion = "Categoria dashboard bulk",
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
            telefono = "+51 999 111 222",
            club = "Club Dashboard",
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

    private async Task<string> CreateInscriptionAsync(string competitorId, string eventId, string categoryId, string paymentMethod)
    {
        var response = await _client.PostAsJsonAsync("/v1/inscriptions", new
        {
            competitorId,
            eventId,
            categoryId,
            shirtNumber = "21",
            paymentMethod,
            reglamento = true,
            riesgosAceptados = true,
            usoImagenAceptado = true
        });

        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == System.Net.HttpStatusCode.Created, responseBody);
        var body = JObject.Parse(responseBody);
        return body["id"]!.Value<string>()!;
    }
}
