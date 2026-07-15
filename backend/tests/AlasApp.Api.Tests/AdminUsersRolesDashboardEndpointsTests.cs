using AlasApp.Application.Abstractions.Services;
using AlasApp.Domain.Entities;
using AlasApp.Domain.Enums;
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
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace AlasApp.Api.Tests;

public sealed class AdminUsersRolesDashboardEndpointsTests : IClassFixture<AdminUsersDashboardWebApplicationFactory>
{
    private readonly AdminUsersDashboardWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AdminUsersRolesDashboardEndpointsTests(AdminUsersDashboardWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AdminUsersCrudAndRoles_ShouldWork()
    {
        await AuthenticateAsAsync(AdminRole.SuperAdmin, $"superadmin-{Guid.NewGuid():N}@test.com");

        var email = $"admin-{Guid.NewGuid():N}@test.com";

        var createResponse = await _client.PostAsJsonAsync("/v1/admin/users", new
        {
            nombre = "Gabriel",
            apellido = "Villani",
            email,
            rol = "Admin",
            sendInvitationEmail = true
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = JObject.Parse(await createResponse.Content.ReadAsStringAsync());
        var userId = created["id"]?.Value<string>();
        Assert.Equal(email, created["email"]?.Value<string>());
        Assert.Equal("Admin", created["role"]?.Value<string>());
        Assert.Equal("Activo", created["status"]?.Value<string>());

        var listResponse = await _client.GetAsync("/v1/admin/users");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var listJson = JObject.Parse(await listResponse.Content.ReadAsStringAsync());
        Assert.Contains(listJson["data"]!, item => item?["email"]?.Value<string>() == email);

        var getResponse = await _client.GetAsync($"/v1/admin/users/{userId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var updateResponse = await _client.PutAsJsonAsync($"/v1/admin/users/{userId}", new
        {
            rol = "Revisor",
            status = "Inactivo"
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = JObject.Parse(await updateResponse.Content.ReadAsStringAsync());
        Assert.Equal("Revisor", updated["role"]?.Value<string>());
        Assert.Equal("Inactivo", updated["status"]?.Value<string>());

        var rolesResponse = await _client.GetAsync("/v1/admin/roles");
        Assert.Equal(HttpStatusCode.OK, rolesResponse.StatusCode);

        var rolesJson = JObject.Parse(await rolesResponse.Content.ReadAsStringAsync());
        Assert.True(rolesJson["data"]?.Count() >= 4);
        Assert.Contains(rolesJson["data"]!, item => item?["name"]?.Value<string>() == "Super Admin");
        Assert.Contains(rolesJson["data"]![0]!["permissions"]!, permission => permission?["module"]?.Value<string>() == "Dashboard");

        var deleteResponse = await _client.DeleteAsync($"/v1/admin/users/{userId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var missingResponse = await _client.GetAsync($"/v1/admin/users/{userId}");
        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
    }

    [Fact]
    public async Task CreateAdminUser_WithDuplicateEmail_ShouldReturnConflict()
    {
        await AuthenticateAsAsync(AdminRole.SuperAdmin, $"dup-super-{Guid.NewGuid():N}@test.com");

        var email = $"dup-admin-{Guid.NewGuid():N}@test.com";

        await _client.PostAsJsonAsync("/v1/admin/users", new
        {
            nombre = "Ana",
            apellido = "Ruiz",
            email,
            rol = "Admin",
            sendInvitationEmail = false
        });

        var duplicateResponse = await _client.PostAsJsonAsync("/v1/admin/users", new
        {
            nombre = "Ana",
            apellido = "Ruiz",
            email,
            rol = "Admin",
            sendInvitationEmail = false
        });

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteAdminUser_ShouldRejectSelfDelete()
    {
        var seededEmail = $"self-delete-{Guid.NewGuid():N}@test.com";
        var seededUserId = await SeedAdminUserAsync(seededEmail, "Password1");

        var loginResponse = await _client.PostAsJsonAsync("/v1/auth/login", new
        {
            email = seededEmail,
            password = "Password1",
            rememberMe = false
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginJson = JObject.Parse(await loginResponse.Content.ReadAsStringAsync());
        var token = loginJson["accessToken"]?.Value<string>();
        Assert.False(string.IsNullOrWhiteSpace(token));

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var deleteResponse = await _client.DeleteAsync($"/v1/admin/users/{seededUserId}");
        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task Dashboard_ShouldReturnAggregatedData()
    {
        await AuthenticateAsAsync(AdminRole.SuperAdmin, $"dashboard-super-{Guid.NewGuid():N}@test.com");

        var circuitId = await CreateCircuitAsync();
        var eventId = await CreateEventAsync(circuitId);
        var categoryId = await CreateCategoryAsync();
        await AssignCategoryAsync(eventId, categoryId);
        var competitorId = await CreateCompetitorAsync("Laura", "Mendez", $"dashboard-{Guid.NewGuid():N}@test.com");
        var inscriptionId = await CreateInscriptionAsync(competitorId, eventId, categoryId, "paypal");

        var paymentResponse = await _client.PostAsJsonAsync("/v1/payments", new
        {
            inscriptionId,
            method = "paypal",
            amountUsd = 95,
            transactionId = $"PP-{Guid.NewGuid():N}".Substring(0, 12)
        });

        var paymentBody = await paymentResponse.Content.ReadAsStringAsync();
        Assert.True(paymentResponse.StatusCode == HttpStatusCode.Created, paymentBody);

        var competitorBeachId = await CreateCompetitorAsync("Mario", "Costa", $"dashboard-beach-{Guid.NewGuid():N}@test.com");
        var beachInscriptionId = await CreateInscriptionAsync(competitorBeachId, eventId, categoryId, "beach");

        var tokenRequestResponse = await _client.PostAsJsonAsync("/v1/payments/beach/request", new
        {
            inscriptionId = beachInscriptionId
        });

        var tokenRequestBody = await tokenRequestResponse.Content.ReadAsStringAsync();
        Assert.True(tokenRequestResponse.StatusCode == HttpStatusCode.Created, tokenRequestBody);

        var dashboardResponse = await _client.GetAsync("/v1/admin/dashboard");
        Assert.Equal(HttpStatusCode.OK, dashboardResponse.StatusCode);

        var dashboardJson = JObject.Parse(await dashboardResponse.Content.ReadAsStringAsync());
        Assert.True(dashboardJson["kpis"]?["totalCompetidores"]?.Value<int>() >= 2);
        Assert.True(dashboardJson["kpis"]?["totalEventosActivos"]?.Value<int>() >= 1);
        Assert.True(dashboardJson["kpis"]?["totalInscripciones"]?.Value<int>() >= 2);
        Assert.True(dashboardJson["kpis"]?["recaudacionMesUsd"]?.Value<double>() >= 95d);
        Assert.True(dashboardJson["kpis"]?["tokensPendientes"]?.Value<int>() >= 1);
        Assert.True(dashboardJson["activeEvents"]?.Count() >= 1);
        Assert.True(dashboardJson["recentInscriptions"]?.Count() >= 1);
    }

    [Fact]
    public async Task AdminEndpoints_WhenAnonymous_ShouldReturnUnauthorized()
    {
        var dashboardResponse = await _client.GetAsync("/v1/admin/dashboard");
        Assert.Equal(HttpStatusCode.Unauthorized, dashboardResponse.StatusCode);

        var usersResponse = await _client.GetAsync("/v1/admin/users");
        Assert.Equal(HttpStatusCode.Unauthorized, usersResponse.StatusCode);

        var rolesResponse = await _client.GetAsync("/v1/admin/roles");
        Assert.Equal(HttpStatusCode.Unauthorized, rolesResponse.StatusCode);
    }

    [Fact]
    public async Task AdminEndpoints_ShouldRespectRolePermissions()
    {
        await AuthenticateAsAsync(AdminRole.Revisor, $"revisor-{Guid.NewGuid():N}@test.com");

        var listUsersResponse = await _client.GetAsync("/v1/admin/users");
        Assert.Equal(HttpStatusCode.OK, listUsersResponse.StatusCode);

        var dashboardResponse = await _client.GetAsync("/v1/admin/dashboard");
        Assert.Equal(HttpStatusCode.OK, dashboardResponse.StatusCode);

        var createUserResponse = await _client.PostAsJsonAsync("/v1/admin/users", new
        {
            nombre = "Prohibido",
            apellido = "Crear",
            email = $"blocked-{Guid.NewGuid():N}@test.com",
            rol = "Admin",
            sendInvitationEmail = false
        });
        Assert.Equal(HttpStatusCode.Forbidden, createUserResponse.StatusCode);

        var rolesResponse = await _client.GetAsync("/v1/admin/roles");
        Assert.Equal(HttpStatusCode.Forbidden, rolesResponse.StatusCode);

        await AuthenticateAsAsync(AdminRole.Arbitro, $"arbitro-{Guid.NewGuid():N}@test.com");

        var usersForbiddenResponse = await _client.GetAsync("/v1/admin/users");
        Assert.Equal(HttpStatusCode.Forbidden, usersForbiddenResponse.StatusCode);

        var dashboardAllowedResponse = await _client.GetAsync("/v1/admin/dashboard");
        Assert.Equal(HttpStatusCode.OK, dashboardAllowedResponse.StatusCode);
    }

    [Fact]
    public async Task GetCurrentAdminProfile_WithMeAlias_ShouldReturnAuthenticatedUser()
    {
        var email = $"revisor-profile-{Guid.NewGuid():N}@test.com";
        await AuthenticateAsAsync(AdminRole.Revisor, email);

        var response = await _client.GetAsync("/v1/admin/users/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = JObject.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(email, payload["email"]?.Value<string>());
        Assert.Equal("Revisor", payload["role"]?.Value<string>());
        Assert.Equal("Activo", payload["status"]?.Value<string>());
    }

    private async Task AuthenticateAsAsync(AdminRole role, string email, string password = "Password1")
    {
        await SeedAdminUserAsync(email, password, role);

        var loginResponse = await _client.PostAsJsonAsync("/v1/auth/login", new
        {
            email,
            password,
            rememberMe = false
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginJson = JObject.Parse(await loginResponse.Content.ReadAsStringAsync());
        var token = loginJson["accessToken"]?.Value<string>();
        Assert.False(string.IsNullOrWhiteSpace(token));

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task<Guid> SeedAdminUserAsync(string email, string password, AdminRole role = AdminRole.SuperAdmin)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AlasAppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var user = UserAccount.Create(
            email,
            passwordHasher.Hash(password),
            "Seeded",
            "Admin",
            UserType.Espectador,
            string.Empty,
            PreferredLanguage.Espanol,
            false,
            true,
            false,
            null,
            role);

        dbContext.UserAccounts.Add(user);
        await dbContext.SaveChangesAsync();
        return user.Id;
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

    private async Task<string> CreateCircuitAsync()
    {
        var response = await _client.PostAsJsonAsync("/v1/circuits", new
        {
            nombre = "Circuito Dashboard",
            temporada = 2026,
            descripcion = "Circuito dashboard",
            region = "Latinoamérica",
            modalidad = "Shortboard",
            estado = "Activo",
            surfScoresCode = $"DASH-{Guid.NewGuid():N}".Substring(0, 12)
        });

        var body = JObject.Parse(await response.Content.ReadAsStringAsync());
        return body["id"]!.Value<string>()!;
    }

    private async Task<string> CreateEventAsync(string circuitId)
    {
        var response = await _client.PostAsJsonAsync("/v1/events", new
        {
            nombre = "Evento Dashboard",
            circuitId,
            fechaInicio = "2026-10-02",
            fechaFin = "2026-10-04",
            pais = "Perú",
            ciudad = "Lima",
            playa = "Punta Rocas",
            stars = 4,
            capacidadMaxima = 150,
            prizeAmountUsd = 20000,
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
            nombre = $"OpenDash{Guid.NewGuid():N}"[..16],
            descripcion = "Categoria dashboard",
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
        return body["id"]!.Value<string>()!;
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
            reglamento = true
        });

        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Created, responseBody);
        var body = JObject.Parse(responseBody);
        return body["id"]!.Value<string>()!;
    }
}

public sealed class AdminUsersDashboardWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"AdminUsersDashboardTests-{Guid.NewGuid():N}";

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
