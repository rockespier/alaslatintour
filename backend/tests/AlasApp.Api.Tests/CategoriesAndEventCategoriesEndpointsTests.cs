using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace AlasApp.Api.Tests;

public sealed class CategoriesAndEventCategoriesEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CategoriesAndEventCategoriesEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CategoryCrudFlow_Works_EndToEnd()
    {
        var successorResponse = await _client.PostAsJsonAsync("/v1/categories", new
        {
            nombre = "Open",
            descripcion = "Categoria principal",
            gender = "Ambos",
            ageRestriction = false,
            minAge = (int?)null,
            maxAge = (int?)null,
            successorCategoryId = (string?)null,
            status = "Activo"
        });

        var successorBody = await successorResponse.Content.ReadAsStringAsync();
        if (successorResponse.StatusCode != HttpStatusCode.Created)
        {
            throw new InvalidOperationException(
                $"Expected 201 Created but received {(int)successorResponse.StatusCode} {successorResponse.StatusCode}. Body: {successorBody}");
        }
        Assert.Equal(HttpStatusCode.Created, successorResponse.StatusCode);

        var successor = JsonDocument.Parse(successorBody);
        var successorId = successor.RootElement.GetProperty("id").GetString();
        Assert.False(string.IsNullOrWhiteSpace(successorId));

        var createResponse = await _client.PostAsJsonAsync("/v1/categories", new
        {
            nombre = "Sub 18",
            descripcion = "Categoria juvenil",
            gender = "Masculino",
            ageRestriction = true,
            minAge = 12,
            maxAge = 18,
            successorCategoryId = successorId,
            status = "Activo"
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await ReadJsonAsync(createResponse);
        var categoryId = created.RootElement.GetProperty("id").GetString();
        Assert.Equal("Sub 18", created.RootElement.GetProperty("nombre").GetString());
        Assert.Equal(successorId, created.RootElement.GetProperty("successorCategoryId").GetString());

        var listResponse = await _client.GetAsync("/v1/categories?status=Activo");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/v1/categories/{categoryId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var updateResponse = await _client.PutAsJsonAsync($"/v1/categories/{categoryId}", new
        {
            nombre = "Sub 18 Updated",
            descripcion = "Categoria juvenil actualizada",
            gender = "Ambos",
            ageRestriction = true,
            minAge = 13,
            maxAge = 18,
            successorCategoryId = successorId,
            status = "Inactivo"
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var deleteSuccessorWhileReferenced = await _client.DeleteAsync($"/v1/categories/{successorId}");
        Assert.Equal(HttpStatusCode.Conflict, deleteSuccessorWhileReferenced.StatusCode);

        var deleteChildResponse = await _client.DeleteAsync($"/v1/categories/{categoryId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteChildResponse.StatusCode);

        var deleteSuccessorResponse = await _client.DeleteAsync($"/v1/categories/{successorId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteSuccessorResponse.StatusCode);
    }

    [Fact]
    public async Task EventCategoriesFlow_Works_EndToEnd()
    {
        var circuitResponse = await _client.PostAsJsonAsync("/v1/circuits", new
        {
            nombre = "Circuito Categorias",
            temporada = 2026,
            descripcion = "Circuito para categorias",
            region = "Latinoamérica",
            modalidad = "Shortboard",
            estado = "Activo",
            surfScoresCode = "CAT-2026"
        });

        var circuitBody = await circuitResponse.Content.ReadAsStringAsync();
        if (circuitResponse.StatusCode != HttpStatusCode.Created)
        {
            throw new InvalidOperationException(
                $"Expected 201 Created but received {(int)circuitResponse.StatusCode} {circuitResponse.StatusCode}. Body: {circuitBody}");
        }

        var circuit = JsonDocument.Parse(circuitBody);
        var circuitId = circuit.RootElement.GetProperty("id").GetString();

        var eventResponse = await _client.PostAsJsonAsync("/v1/events", new
        {
            nombre = "Evento Categorias",
            circuitId,
            fechaInicio = "2026-08-10",
            fechaFin = "2026-08-12",
            pais = "Perú",
            ciudad = "Lobitos",
            playa = "Lobitos",
            stars = 4,
            capacidadMaxima = 120,
            prizeAmountUsd = 10000,
            surfScoresCode = "EV-CAT-2026",
            accessType = "Abierto",
            estado = "Activo"
        });

        var createdEvent = await ReadJsonAsync(eventResponse);
        var eventId = createdEvent.RootElement.GetProperty("id").GetString();

        var categoryAResponse = await _client.PostAsJsonAsync("/v1/categories", new
        {
            nombre = "Open Mujeres",
            descripcion = "Categoria abierta",
            gender = "Femenino",
            ageRestriction = false,
            minAge = (int?)null,
            maxAge = (int?)null,
            successorCategoryId = (string?)null,
            status = "Activo"
        });

        var categoryBResponse = await _client.PostAsJsonAsync("/v1/categories", new
        {
            nombre = "Open Hombres",
            descripcion = "Categoria abierta",
            gender = "Masculino",
            ageRestriction = false,
            minAge = (int?)null,
            maxAge = (int?)null,
            successorCategoryId = (string?)null,
            status = "Activo"
        });

        var categoryA = await ReadJsonAsync(categoryAResponse);
        var categoryB = await ReadJsonAsync(categoryBResponse);
        var categoryAId = categoryA.RootElement.GetProperty("id").GetString();
        var categoryBId = categoryB.RootElement.GetProperty("id").GetString();

        var initialGet = await _client.GetAsync($"/v1/events/{eventId}/categories");
        Assert.Equal(HttpStatusCode.OK, initialGet.StatusCode);

        var initialBody = await ReadJsonAsync(initialGet);
        Assert.True(initialBody.RootElement.GetProperty("useCircuitTariffs").GetBoolean());
        Assert.Equal(0, initialBody.RootElement.GetProperty("data").GetArrayLength());

        var updateResponse = await _client.PutAsJsonAsync($"/v1/events/{eventId}/categories", new
        {
            useCircuitTariffs = false,
            categories = new[]
            {
                new
                {
                    categoryId = categoryAId,
                    customTariffUsd = 85,
                    customTariffCop = 340000,
                    capacidad = 32
                },
                new
                {
                    categoryId = categoryBId,
                    customTariffUsd = 90,
                    customTariffCop = 360000,
                    capacidad = 40
                }
            }
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var getAfterUpdate = await _client.GetAsync($"/v1/events/{eventId}/categories");
        Assert.Equal(HttpStatusCode.OK, getAfterUpdate.StatusCode);

        var updated = await ReadJsonAsync(getAfterUpdate);
        Assert.False(updated.RootElement.GetProperty("useCircuitTariffs").GetBoolean());
        Assert.Equal(2, updated.RootElement.GetProperty("data").GetArrayLength());

        var firstCategory = updated.RootElement.GetProperty("data")[0];
        Assert.True(firstCategory.GetProperty("effectiveTariffUsd").GetDouble() > 0);

        var deleteAssignedCategory = await _client.DeleteAsync($"/v1/categories/{categoryAId}");
        Assert.Equal(HttpStatusCode.Conflict, deleteAssignedCategory.StatusCode);

        var clearAssignmentsResponse = await _client.PutAsJsonAsync($"/v1/events/{eventId}/categories", new
        {
            useCircuitTariffs = true,
            categories = Array.Empty<object>()
        });

        Assert.Equal(HttpStatusCode.OK, clearAssignmentsResponse.StatusCode);

        var deleteCategoryAResponse = await _client.DeleteAsync($"/v1/categories/{categoryAId}");
        var deleteCategoryBResponse = await _client.DeleteAsync($"/v1/categories/{categoryBId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteCategoryAResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, deleteCategoryBResponse.StatusCode);
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(content);
    }
}
