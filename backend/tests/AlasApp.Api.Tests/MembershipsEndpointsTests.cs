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
using System.Net.Http.Json;
using Xunit;

namespace AlasApp.Api.Tests;

public sealed class MembershipsEndpointsTests : IClassFixture<MembershipsWebApplicationFactory>
{
    private readonly MembershipsWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public MembershipsEndpointsTests(MembershipsWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task MembershipsCrud_ShouldWork()
    {
        var createResponse = await _client.PostAsJsonAsync("/v1/memberships", new
        {
            clubFederacion = "Federacion Peruana de Surf",
            pais = "Perú",
            plan = "Mensual",
            inicioVigencia = "2026-07-01",
            vencimiento = "2026-10-15",
            emailContacto = "membresias@alas.test"
        });

        var createBody = await createResponse.Content.ReadAsStringAsync();
        Assert.True(createResponse.StatusCode == HttpStatusCode.Created, createBody);

        var created = JObject.Parse(createBody);
        var membershipId = created["id"]?.Value<string>();
        Assert.Equal("Federacion Peruana de Surf", created["clubFederacion"]?.Value<string>());
        Assert.Equal("Mensual", created["plan"]?.Value<string>());
        Assert.Equal("Activo", created["estado"]?.Value<string>());

        var listResponse = await _client.GetAsync("/v1/memberships?page=1&limit=20&status=Activo");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var listJson = JObject.Parse(await listResponse.Content.ReadAsStringAsync());
        Assert.Contains(listJson["data"]!, item => item?["id"]?.Value<string>() == membershipId);

        var getResponse = await _client.GetAsync($"/v1/memberships/{membershipId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var updateResponse = await _client.PutAsJsonAsync($"/v1/memberships/{membershipId}", new
        {
            clubFederacion = "Federacion Peruana de Surf",
            pais = "Perú",
            plan = "Por evento",
            inicioVigencia = "2026-07-01",
            vencimiento = "2026-07-20",
            emailContacto = "contacto@federacion.test"
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updated = JObject.Parse(await updateResponse.Content.ReadAsStringAsync());
        Assert.Equal("Por evento", updated["plan"]?.Value<string>());
        Assert.Equal("Vence pronto", updated["estado"]?.Value<string>());
        Assert.Equal("contacto@federacion.test", updated["emailContacto"]?.Value<string>());

        var deleteResponse = await _client.DeleteAsync($"/v1/memberships/{membershipId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var missingResponse = await _client.GetAsync($"/v1/memberships/{membershipId}");
        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
    }

    [Fact]
    public async Task Memberships_ShouldPopulateAffiliatedCompetitors()
    {
        await TestAdminAuthHelper.AuthenticateAsAdminAsync(_client, _factory.Services);

        await CreateCompetitorAsync(
            "Lucia",
            "Ramos",
            $"lucia-{Guid.NewGuid():N}@test.com",
            "Perú",
            "Federacion Peruana de Surf",
            "Otro");

        await CreateCompetitorAsync(
            "Mateo",
            "Costa",
            $"mateo-{Guid.NewGuid():N}@test.com",
            "Perú",
            "Otro Club",
            "Federacion Peruana de Surf");

        await CreateCompetitorAsync(
            "Nora",
            "Diaz",
            $"nora-{Guid.NewGuid():N}@test.com",
            "Chile",
            "Federacion Peruana de Surf",
            "Federacion Peruana de Surf");

        var createResponse = await _client.PostAsJsonAsync("/v1/memberships", new
        {
            clubFederacion = "Federacion Peruana de Surf",
            pais = "Perú",
            plan = "Mensual",
            inicioVigencia = "2026-07-01",
            vencimiento = "2026-09-30",
            emailContacto = "afiliaciones@alas.test"
        });

        var createBody = await createResponse.Content.ReadAsStringAsync();
        Assert.True(createResponse.StatusCode == HttpStatusCode.Created, createBody);

        var created = JObject.Parse(createBody);
        var membershipId = created["id"]?.Value<string>();
        Assert.Equal(2, created["competidoresAfiliados"]?.Value<int>());

        var getResponse = await _client.GetAsync($"/v1/memberships/{membershipId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var getJson = JObject.Parse(await getResponse.Content.ReadAsStringAsync());
        Assert.Equal(2, getJson["competidoresAfiliados"]?.Value<int>());
    }

    private async Task CreateCompetitorAsync(
        string nombre,
        string apellido,
        string email,
        string pais,
        string club,
        string federacion)
    {
        var response = await _client.PostAsJsonAsync("/v1/competitors", new
        {
            nombre,
            apellido,
            email,
            fechaNacimiento = "1997-05-12",
            genero = "Femenino",
            pais,
            telefono = "+51 999 888 777",
            club,
            postura = "Regular",
            tallaCamiseta = "M",
            numeroCamiseta = "7",
            federacion,
            patrocinadores = "Marca Test"
        });

        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Created, body);
    }
}

public sealed class MembershipsWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"MembershipsTests-{Guid.NewGuid():N}";

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
