using AlasApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace AlasApp.Api.Tests;

public sealed class AuthEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task RegisterCompetitorAndLogin_ShouldReturnJwt()
    {
        var email = $"competidor-{Guid.NewGuid():N}@test.com";

        var registerResponse = await _client.PostAsJsonAsync("/v1/auth/register", new
        {
            email,
            password = "Password1",
            nombre = "Gabriel",
            apellido = "Villani",
            tipo = "competidor",
            pais = "Brasil",
            idiomaPreferido = "Español",
            newsletter = true,
            terminos = true,
            reglamento = true,
            fechaNacimiento = "1998-04-22",
            genero = "Masculino",
            telefono = "+55 11 98765-4321",
            club = "ALAS Club",
            postura = "Regular",
            tallaCamiseta = "M",
            federacion = "CBSurf",
            patrocinadores = "Marca X"
        });

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var registerJson = JObject.Parse(await registerResponse.Content.ReadAsStringAsync());
        Assert.Equal(email, registerJson["email"]?.Value<string>());
        Assert.Equal("competidor", registerJson["tipo"]?.Value<string>());
        Assert.Equal("Pendiente de validación", registerJson["licenseStatus"]?.Value<string>());

        var loginResponse = await _client.PostAsJsonAsync("/v1/auth/login", new
        {
            email,
            password = "Password1",
            rememberMe = false
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginJson = JObject.Parse(await loginResponse.Content.ReadAsStringAsync());
        Assert.False(string.IsNullOrWhiteSpace(loginJson["accessToken"]?.Value<string>()));
        Assert.Equal(email, loginJson["user"]?["email"]?.Value<string>());
        Assert.Equal("competidor", loginJson["user"]?["tipo"]?.Value<string>());
    }

    [Fact]
    public async Task Logout_ShouldInvalidateCurrentToken()
    {
        var email = $"viewer-{Guid.NewGuid():N}@test.com";

        await RegisterCompetitorAsync(email, "Password1");

        var loginResponse = await _client.PostAsJsonAsync("/v1/auth/login", new
        {
            email,
            password = "Password1",
            rememberMe = false
        });

        var loginJson = JObject.Parse(await loginResponse.Content.ReadAsStringAsync());
        var token = loginJson["accessToken"]?.Value<string>();

        Assert.False(string.IsNullOrWhiteSpace(token));

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var logoutResponse = await _client.PostAsync("/v1/auth/logout", null);
        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);

        var secondLogoutResponse = await _client.PostAsync("/v1/auth/logout", null);
        Assert.Equal(HttpStatusCode.Unauthorized, secondLogoutResponse.StatusCode);
    }

    [Fact]
    public async Task RequestPasswordReset_ShouldReturnOkForUnknownEmail()
    {
        var response = await _client.PostAsJsonAsync("/v1/auth/password-reset/request", new
        {
            email = $"unknown-{Guid.NewGuid():N}@test.com"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ConfirmPasswordReset_WithInvalidToken_ShouldReturnBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/v1/auth/password-reset/confirm", new
        {
            token = "invalid-token",
            newPassword = "Password2"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RequestPasswordReset_ShouldPersistTokenForExistingUser()
    {
        var email = $"reset-{Guid.NewGuid():N}@test.com";

        await RegisterCompetitorAsync(email, "Password1");

        var response = await _client.PostAsJsonAsync("/v1/auth/password-reset/request", new { email });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AlasAppDbContext>();

        var persisted = dbContext.PasswordResetTokens.Any();
        Assert.True(persisted);
    }

    [Fact]
    public async Task Login_ShouldLockAccountAfterThreeFailedAttempts()
    {
        var email = $"lock-{Guid.NewGuid():N}@test.com";
        const string password = "Password1";

        await RegisterCompetitorAsync(email, password);

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var failedResponse = await _client.PostAsJsonAsync("/v1/auth/login", new
            {
                email,
                password = "WrongPassword1",
                rememberMe = false
            });

            Assert.Equal(HttpStatusCode.Unauthorized, failedResponse.StatusCode);
        }

        var correctWhileLockedResponse = await _client.PostAsJsonAsync("/v1/auth/login", new
        {
            email,
            password,
            rememberMe = false
        });

        Assert.Equal(HttpStatusCode.Unauthorized, correctWhileLockedResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AlasAppDbContext>();
        var user = await dbContext.UserAccounts.SingleAsync(x => x.Email == email);

        Assert.Equal(3, user.FailedLoginAttempts);
        Assert.True(user.LockedUntilUtc.HasValue);
        Assert.True(user.LockedUntilUtc.Value > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Login_ShouldResetFailedAttemptsAfterSuccessfulAuthentication()
    {
        var email = $"reset-login-{Guid.NewGuid():N}@test.com";
        const string password = "Password1";

        await RegisterCompetitorAsync(email, password);

        for (var attempt = 0; attempt < 2; attempt++)
        {
            var failedResponse = await _client.PostAsJsonAsync("/v1/auth/login", new
            {
                email,
                password = "WrongPassword1",
                rememberMe = false
            });

            Assert.Equal(HttpStatusCode.Unauthorized, failedResponse.StatusCode);
        }

        var successResponse = await _client.PostAsJsonAsync("/v1/auth/login", new
        {
            email,
            password,
            rememberMe = false
        });

        Assert.Equal(HttpStatusCode.OK, successResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AlasAppDbContext>();
        var user = await dbContext.UserAccounts.SingleAsync(x => x.Email == email);

        Assert.Equal(0, user.FailedLoginAttempts);
        Assert.Null(user.LockedUntilUtc);
    }

    [Fact]
    public async Task Login_ShouldAllowAccessAfterLockExpires()
    {
        var email = $"unlock-{Guid.NewGuid():N}@test.com";
        const string password = "Password1";

        await RegisterCompetitorAsync(email, password);

        for (var attempt = 0; attempt < 3; attempt++)
        {
            await _client.PostAsJsonAsync("/v1/auth/login", new
            {
                email,
                password = "WrongPassword1",
                rememberMe = false
            });
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AlasAppDbContext>();
            var user = await dbContext.UserAccounts.SingleAsync(x => x.Email == email);
            user.SetUpdated(DateTimeOffset.UtcNow);
            typeof(AlasApp.Domain.Entities.UserAccount)
                .GetProperty(nameof(AlasApp.Domain.Entities.UserAccount.LockedUntilUtc))!
                .SetValue(user, DateTimeOffset.UtcNow.AddMinutes(-1));
            await dbContext.SaveChangesAsync();
        }

        var successResponse = await _client.PostAsJsonAsync("/v1/auth/login", new
        {
            email,
            password,
            rememberMe = false
        });

        Assert.Equal(HttpStatusCode.OK, successResponse.StatusCode);
    }

    private async Task RegisterCompetitorAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/v1/auth/register", new
        {
            email,
            password,
            nombre = "Ana",
            apellido = "Ruiz",
            tipo = "competidor",
            pais = "Perú",
            idiomaPreferido = "Español",
            newsletter = false,
            terminos = true,
            reglamento = true,
            fechaNacimiento = "1998-04-22",
            genero = "Femenino",
            telefono = "+51 999 111 222",
            club = "ALAS Club",
            postura = "Regular",
            tallaCamiseta = "M",
            federacion = "FENTA",
            patrocinadores = "Marca X"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
