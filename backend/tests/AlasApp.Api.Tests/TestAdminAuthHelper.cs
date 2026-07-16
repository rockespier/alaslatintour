using AlasApp.Domain.Entities;
using AlasApp.Domain.Enums;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Infrastructure.Authentication;
using AlasApp.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace AlasApp.Api.Tests;

internal static class TestAdminAuthHelper
{
    public static async Task AuthenticateAsAdminAsync(
        HttpClient client,
        IServiceProvider services,
        AdminRole role = AdminRole.SuperAdmin,
        string password = "Password1")
    {
        var email = $"admin-{role}-{Guid.NewGuid():N}@test.com".ToLowerInvariant();

        using (var scope = services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AlasAppDbContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            var user = UserAccount.Create(
                email,
                passwordHasher.Hash(password),
                "Test",
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
        }

        var loginResponse = await client.PostAsJsonAsync("/v1/auth/login", new
        {
            email,
            password,
            rememberMe = false
        });

        if (loginResponse.StatusCode != HttpStatusCode.OK)
        {
            var body = await loginResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"No se pudo autenticar el admin de prueba. Status: {(int)loginResponse.StatusCode}. Body: {body}");
        }

        var loginJson = JObject.Parse(await loginResponse.Content.ReadAsStringAsync());
        var token = loginJson["accessToken"]?.Value<string>();
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("El login de admin de prueba no devolvio accessToken.");
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
