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
using System.Text;
using Xunit;

namespace AlasApp.Api.Tests;

public sealed class AdminSettingsEndpointsTests : IClassFixture<AdminSettingsWebApplicationFactory>
{
    private readonly AdminSettingsWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AdminSettingsEndpointsTests(AdminSettingsWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetSettings_ShouldReturnDefaults()
    {
        await AuthenticateAsAsync(AdminRole.SuperAdmin, $"settings-super-{Guid.NewGuid():N}@test.com");

        var response = await _client.GetAsync("/v1/admin/settings");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = JObject.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("ALAS Latin Tour", json["general"]?["organizationName"]?.Value<string>());
        Assert.Equal(24, json["notifications"]?["tokenValidityHours"]?.Value<int>());
        Assert.Equal(8, json["ranking"]?["pointsMatrix"]?.Count());
        Assert.Equal(5, json["live"]?["surfScores"]?["refreshMinutes"]?.Value<int>());
    }

    [Fact]
    public async Task PutSettings_ShouldPersistConfiguration()
    {
        await AuthenticateAsAsync(AdminRole.SuperAdmin, $"settings-update-{Guid.NewGuid():N}@test.com");

        var defaultsResponse = await _client.GetAsync("/v1/admin/settings");
        var settings = JObject.Parse(await defaultsResponse.Content.ReadAsStringAsync());
        settings["general"]!["organizationName"] = "ALAS Global Tour";
        settings["general"]!["season"]!["currentYear"] = 2027;
        settings["live"]!["youtube"]!["active"] = true;
        settings["live"]!["youtube"]!["videoIdOrUrl"] = "dQw4w9WgXcQ";

        var updateResponse = await PutSettingsAsync(settings);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var getResponse = await _client.GetAsync("/v1/admin/settings");
        var persisted = JObject.Parse(await getResponse.Content.ReadAsStringAsync());
        Assert.Equal("ALAS Global Tour", persisted["general"]?["organizationName"]?.Value<string>());
        Assert.Equal(2027, persisted["general"]?["season"]?["currentYear"]?.Value<int>());
        Assert.True(persisted["live"]?["youtube"]?["active"]?.Value<bool>());
    }

    [Fact]
    public async Task PutSettings_WithInvalidSurfScoresRefresh_ShouldReturnBadRequest()
    {
        await AuthenticateAsAsync(AdminRole.SuperAdmin, $"settings-invalid-{Guid.NewGuid():N}@test.com");

        var defaultsResponse = await _client.GetAsync("/v1/admin/settings");
        var settings = JObject.Parse(await defaultsResponse.Content.ReadAsStringAsync());
        settings["live"]!["surfScores"]!["refreshMinutes"] = 1;

        var updateResponse = await PutSettingsAsync(settings);

        Assert.Equal(HttpStatusCode.BadRequest, updateResponse.StatusCode);
    }

    [Fact]
    public async Task TestIntegration_ShouldReturnConnectedWhenConfigured()
    {
        await AuthenticateAsAsync(AdminRole.SuperAdmin, $"settings-test-{Guid.NewGuid():N}@test.com");

        var response = await _client.PostAsync("/v1/admin/settings/integrations/surfscores/test", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = JObject.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("surfscores", json["provider"]?.Value<string>());
        Assert.Equal("connected", json["status"]?.Value<string>());
    }

    [Fact]
    public async Task AdminRole_ShouldReadButNotWriteSettings()
    {
        await AuthenticateAsAsync(AdminRole.Admin, $"settings-admin-{Guid.NewGuid():N}@test.com");

        var getResponse = await _client.GetAsync("/v1/admin/settings");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var settings = JObject.Parse(await getResponse.Content.ReadAsStringAsync());
        var updateResponse = await PutSettingsAsync(settings);

        Assert.Equal(HttpStatusCode.Forbidden, updateResponse.StatusCode);
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

    private async Task<HttpResponseMessage> PutSettingsAsync(JObject settings)
    {
        using var content = new StringContent(settings.ToString(), Encoding.UTF8, "application/json");
        return await _client.PutAsync("/v1/admin/settings", content);
    }

    private async Task SeedAdminUserAsync(string email, string password, AdminRole role)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AlasAppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var user = UserAccount.Create(
            email,
            passwordHasher.Hash(password),
            "Settings",
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
}

public sealed class AdminSettingsWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"AdminSettingsTests-{Guid.NewGuid():N}";

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
