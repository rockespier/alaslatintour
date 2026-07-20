using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace AlasApp.Api.Tests;

public sealed class CategoryTariffsAndCompetitorsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CategoryTariffsAndCompetitorsEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CategoryTariffsFlow_Works_EndToEnd()
    {
        await TestAdminAuthHelper.AuthenticateAsAdminAsync(_client, _factory.Services);

        var categoryResponse = await _client.PostAsJsonAsync("/v1/categories", new
        {
            nombre = "Open Tariffs",
            descripcion = "Categoria con tarifas",
            gender = "Ambos",
            ageRestriction = false,
            minAge = (int?)null,
            maxAge = (int?)null,
            successorCategoryId = (string?)null,
            status = "Activo"
        });

        Assert.Equal(HttpStatusCode.Created, categoryResponse.StatusCode);

        var category = await ReadJsonAsync(categoryResponse);
        var categoryId = category.RootElement.GetProperty("id").GetString();

        var initialTariffsResponse = await _client.GetAsync($"/v1/categories/{categoryId}/tariffs");
        Assert.Equal(HttpStatusCode.OK, initialTariffsResponse.StatusCode);

        var initialTariffs = await ReadJsonAsync(initialTariffsResponse);
        Assert.Equal(7, initialTariffs.RootElement.GetProperty("data").GetArrayLength());

        var updateTariffResponse = await _client.PutAsJsonAsync($"/v1/categories/{categoryId}/tariffs/4", new
        {
            usd = 95,
            cop = 380000,
            active = true
        });

        Assert.Equal(HttpStatusCode.OK, updateTariffResponse.StatusCode);

        var updatedTariff = await ReadJsonAsync(updateTariffResponse);
        Assert.Equal(4, updatedTariff.RootElement.GetProperty("starLevel").GetInt32());
        Assert.Equal(95, updatedTariff.RootElement.GetProperty("usd").GetDouble());

        var tariffsAfterUpdateResponse = await _client.GetAsync($"/v1/categories/{categoryId}/tariffs");
        var tariffsAfterUpdate = await ReadJsonAsync(tariffsAfterUpdateResponse);
        var starFourTariff = tariffsAfterUpdate.RootElement
            .GetProperty("data")
            .EnumerateArray()
            .First(x => x.GetProperty("starLevel").GetInt32() == 4);

        Assert.True(starFourTariff.GetProperty("active").GetBoolean());
        Assert.Equal(380000, starFourTariff.GetProperty("cop").GetDouble());
    }

    [Fact]
    public async Task CompetitorCrudFlow_Works_EndToEnd()
    {
        await TestAdminAuthHelper.AuthenticateAsAdminAsync(_client, _factory.Services);

        var createResponse = await _client.PostAsJsonAsync("/v1/competitors", new
        {
            nombre = "Lucas",
            apellido = "Silva",
            email = "lucas.silva@example.com",
            fechaNacimiento = "1998-04-22",
            genero = "Masculino",
            pais = "Brasil",
            telefono = "+55 11 9 8765-4321",
            club = "Praia Club",
            postura = "Regular",
            tallaCamiseta = "M",
            numeroCamiseta = "#23",
            patrocinadores = "Marca A",
            federacion = "CBSurf"
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await ReadJsonAsync(createResponse);
        var competitorId = created.RootElement.GetProperty("id").GetString();
        Assert.Equal("Lucas", created.RootElement.GetProperty("nombre").GetString());
        Assert.Equal("Pendiente de validación", created.RootElement.GetProperty("license").GetProperty("status").GetString());

        var listResponse = await _client.GetAsync("/v1/competitors?page=1&limit=10&country=Brasil&licenseStatus=Pendiente%20de%20validaci%C3%B3n&search=Lucas");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var listBody = await ReadJsonAsync(listResponse);
        Assert.Equal(1, listBody.RootElement.GetProperty("data").GetArrayLength());

        var getResponse = await _client.GetAsync($"/v1/competitors/{competitorId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var updateResponse = await _client.PutAsJsonAsync($"/v1/competitors/{competitorId}", new
        {
            nombre = "Lucas Updated",
            apellido = "Silva",
            email = "lucas.updated@example.com",
            fechaNacimiento = "1998-04-22",
            genero = "Masculino",
            pais = "Brasil",
            telefono = "+55 11 9 0000-0000",
            club = "Praia Club",
            postura = "Goofy",
            tallaCamiseta = "L",
            numeroCamiseta = "#77",
            patrocinadores = "Marca B",
            federacion = "CBSurf"
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updated = await ReadJsonAsync(updateResponse);
        Assert.Equal("Lucas Updated", updated.RootElement.GetProperty("nombre").GetString());
        Assert.Equal("Goofy", updated.RootElement.GetProperty("postura").GetString());

        var duplicateResponse = await _client.PostAsJsonAsync("/v1/competitors", new
        {
            nombre = "Pedro",
            apellido = "Costa",
            email = "lucas.updated@example.com",
            fechaNacimiento = "1999-03-10",
            genero = "Masculino",
            pais = "Brasil",
            telefono = "+55 11 9 1111-1111",
            club = "Outro Club",
            postura = "Regular",
            tallaCamiseta = "S",
            numeroCamiseta = "#09",
            patrocinadores = "Marca C",
            federacion = "CBSurf"
        });

        Assert.Equal(HttpStatusCode.BadRequest, duplicateResponse.StatusCode);

        var deleteResponse = await _client.DeleteAsync($"/v1/competitors/{competitorId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getAfterDeleteResponse = await _client.GetAsync($"/v1/competitors/{competitorId}");
        Assert.Equal(HttpStatusCode.NotFound, getAfterDeleteResponse.StatusCode);
    }

    [Fact]
    public async Task CompetitorAdditionalEndpoints_Work_EndToEnd()
    {
        await TestAdminAuthHelper.AuthenticateAsAdminAsync(_client, _factory.Services);

        var createResponse = await _client.PostAsJsonAsync("/v1/competitors", new
        {
            nombre = "Maria",
            apellido = "Perez",
            email = "maria.perez@example.com",
            fechaNacimiento = "2000-06-15",
            genero = "Femenino",
            pais = "Perú",
            telefono = "+51 999 888 777",
            club = "Lima Surf Club",
            postura = "Regular",
            tallaCamiseta = "S",
            numeroCamiseta = "#12",
            patrocinadores = "Marca D",
            federacion = "FENTA"
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await ReadJsonAsync(createResponse);
        var competitorId = created.RootElement.GetProperty("id").GetString();

        var updateLicenseResponse = await _client.PutAsJsonAsync($"/v1/competitors/{competitorId}/license", new
        {
            status = "Activa",
            licenseNumber = "PE-2026-0012",
            expirationDate = "2026-12-31",
            enabledCategories = new[] { "open-mujeres", "longboard" }
        });

        Assert.Equal(HttpStatusCode.OK, updateLicenseResponse.StatusCode);

        var updatedLicense = await ReadJsonAsync(updateLicenseResponse);
        Assert.Equal("Activa", updatedLicense.RootElement.GetProperty("license").GetProperty("status").GetString());
        Assert.Equal("PE-2026-0012", updatedLicense.RootElement.GetProperty("license").GetProperty("number").GetString());
        Assert.Equal(2, updatedLicense.RootElement.GetProperty("license").GetProperty("enabledCategories").GetArrayLength());

        var getNotificationsResponse = await _client.GetAsync($"/v1/competitors/{competitorId}/notifications");
        Assert.Equal(HttpStatusCode.OK, getNotificationsResponse.StatusCode);

        var notifications = await ReadJsonAsync(getNotificationsResponse);
        Assert.True(notifications.RootElement.GetProperty("email").GetBoolean());
        Assert.True(notifications.RootElement.GetProperty("tokens").GetBoolean());

        var updateNotificationsResponse = await _client.PutAsJsonAsync($"/v1/competitors/{competitorId}/notifications", new
        {
            email = false,
            push = true,
            resultados = false,
            inscripciones = true
        });

        Assert.Equal(HttpStatusCode.OK, updateNotificationsResponse.StatusCode);

        var updatedNotifications = await ReadJsonAsync(updateNotificationsResponse);
        Assert.False(updatedNotifications.RootElement.GetProperty("email").GetBoolean());
        Assert.True(updatedNotifications.RootElement.GetProperty("push").GetBoolean());
        Assert.False(updatedNotifications.RootElement.GetProperty("resultados").GetBoolean());

        var inscriptionsResponse = await _client.GetAsync($"/v1/competitors/{competitorId}/inscriptions?status=Confirmado");
        Assert.Equal(HttpStatusCode.OK, inscriptionsResponse.StatusCode);

        var inscriptions = await ReadJsonAsync(inscriptionsResponse);
        Assert.Equal(0, inscriptions.RootElement.GetProperty("data").GetArrayLength());

        var pointsHistoryResponse = await _client.GetAsync($"/v1/competitors/{competitorId}/points-history?year=2026&categoryId=open-mujeres");
        Assert.Equal(HttpStatusCode.OK, pointsHistoryResponse.StatusCode);

        var pointsHistory = await ReadJsonAsync(pointsHistoryResponse);
        Assert.Equal("SurfScores", pointsHistory.RootElement.GetProperty("attribution").GetString());
        Assert.Equal(0, pointsHistory.RootElement.GetProperty("data").GetArrayLength());

        var calendarResponse = await _client.GetAsync($"/v1/competitors/{competitorId}/calendar");
        Assert.Equal(HttpStatusCode.OK, calendarResponse.StatusCode);

        var calendar = await ReadJsonAsync(calendarResponse);
        Assert.Equal(0, calendar.RootElement.GetProperty("data").GetArrayLength());

        var exportResponse = await _client.GetAsync($"/v1/competitors/{competitorId}/calendar/export");
        Assert.Equal(HttpStatusCode.OK, exportResponse.StatusCode);
        Assert.Equal("text/calendar", exportResponse.Content.Headers.ContentType?.MediaType);

        var exportBody = await exportResponse.Content.ReadAsStringAsync();
        Assert.Contains("BEGIN:VCALENDAR", exportBody);
        Assert.Contains("END:VCALENDAR", exportBody);
    }

    [Fact]
    public async Task CompetitorPasswordAndFines_ShouldWork_EndToEnd()
    {
        var competitorEmail = $"fine-competitor-{Guid.NewGuid():N}@test.com";
        var competitorId = await RegisterCompetitorAsync(competitorEmail, "Password1");

        await TestAdminAuthHelper.AuthenticateAsAdminAsync(_client, _factory.Services);

        var passwordResponse = await _client.PostAsJsonAsync($"/v1/competitors/{competitorId}/password", new
        {
            newPassword = "Password2"
        });

        Assert.Equal(HttpStatusCode.OK, passwordResponse.StatusCode);

        _client.DefaultRequestHeaders.Authorization = null;

        var loginResponse = await _client.PostAsJsonAsync("/v1/auth/login", new
        {
            email = competitorEmail,
            password = "Password2",
            rememberMe = false
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        await TestAdminAuthHelper.AuthenticateAsAdminAsync(_client, _factory.Services);

        var createFineResponse = await _client.PostAsJsonAsync($"/v1/competitors/{competitorId}/fines", new
        {
            amountUsd = 35.5m,
            reason = "Late check-in",
            notes = "Applied by staff"
        });

        Assert.Equal(HttpStatusCode.Created, createFineResponse.StatusCode);

        var createdFine = await ReadJsonAsync(createFineResponse);
        var fineId = createdFine.RootElement.GetProperty("id").GetString();
        Assert.Equal("Pendiente", createdFine.RootElement.GetProperty("status").GetString());

        var listFinesResponse = await _client.GetAsync($"/v1/competitors/{competitorId}/fines");
        Assert.Equal(HttpStatusCode.OK, listFinesResponse.StatusCode);

        var fines = await ReadJsonAsync(listFinesResponse);
        Assert.Equal(1, fines.RootElement.GetArrayLength());

        var updateFineResponse = await _client.PutAsJsonAsync($"/v1/competitors/{competitorId}/fines/{fineId}", new
        {
            amountUsd = 35.5m,
            reason = "Late check-in",
            notes = "Paid at venue",
            status = "Pagada"
        });

        Assert.Equal(HttpStatusCode.OK, updateFineResponse.StatusCode);

        var updatedFine = await ReadJsonAsync(updateFineResponse);
        Assert.Equal("Pagada", updatedFine.RootElement.GetProperty("status").GetString());
        Assert.Equal("Paid at venue", updatedFine.RootElement.GetProperty("notes").GetString());
    }

    private async Task<string> RegisterCompetitorAsync(string email, string password)
    {
        var registerResponse = await TestCompetitorRegistration.PostAsync(
            _client, email, password, "Competidor", "Test");

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var loginResponse = await _client.PostAsJsonAsync("/v1/auth/login", new
        {
            email,
            password,
            rememberMe = false
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var loginJson = await ReadJsonAsync(loginResponse);
        return loginJson.RootElement.GetProperty("user").GetProperty("competitorId").GetString()!;
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(content);
    }
}
