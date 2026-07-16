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
            status = "Activo",
            membresiaAnualUsd = 25,
            membresiaPorEventoUsd = 10
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
            status = "Activo",
            membresiaAnualUsd = 30,
            membresiaPorEventoUsd = 12
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await ReadJsonAsync(createResponse);
        var categoryId = created.RootElement.GetProperty("id").GetString();
        Assert.Equal("Sub 18", created.RootElement.GetProperty("nombre").GetString());
        Assert.Equal(successorId, created.RootElement.GetProperty("successorCategoryId").GetString());
        Assert.Equal(30, created.RootElement.GetProperty("membresiaAnualUsd").GetDouble());
        Assert.Equal(12, created.RootElement.GetProperty("membresiaPorEventoUsd").GetDouble());

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
            status = "Inactivo",
            membresiaAnualUsd = 45,
            membresiaPorEventoUsd = 18
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updatedCategory = await ReadJsonAsync(updateResponse);
        Assert.Equal(45, updatedCategory.RootElement.GetProperty("membresiaAnualUsd").GetDouble());
        Assert.Equal(18, updatedCategory.RootElement.GetProperty("membresiaPorEventoUsd").GetDouble());

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
                    capacidad = 32
                },
                new
                {
                    categoryId = categoryBId,
                    customTariffUsd = 90,
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

    [Fact]
    public async Task EventCategoriesFlow_PerCategoryStars_ResolvesEffectiveTariffIndependentlyOfEventStars()
    {
        var circuitResponse = await _client.PostAsJsonAsync("/v1/circuits", new
        {
            nombre = "Circuito Estrellas",
            temporada = 2026,
            descripcion = "Circuito para estrellas por categoria",
            region = "Latinoamérica",
            modalidad = "Shortboard",
            estado = "Activo",
            surfScoresCode = "STARS-2026"
        });

        var circuit = await ReadJsonAsync(circuitResponse);
        var circuitId = circuit.RootElement.GetProperty("id").GetString();

        // El evento tiene 3 estrellas, pero cada categoria habilitada tendra su propio nivel.
        var eventResponse = await _client.PostAsJsonAsync("/v1/events", new
        {
            nombre = "Evento Estrellas por Categoria",
            circuitId,
            fechaInicio = "2026-09-10",
            fechaFin = "2026-09-12",
            pais = "Perú",
            ciudad = "Lobitos",
            playa = "Lobitos",
            stars = 3,
            capacidadMaxima = 120,
            prizeAmountUsd = 10000,
            surfScoresCode = "EV-STARS-2026",
            accessType = "Abierto",
            estado = "Activo"
        });

        var createdEvent = await ReadJsonAsync(eventResponse);
        var eventId = createdEvent.RootElement.GetProperty("id").GetString();

        var categoryResponse = await _client.PostAsJsonAsync("/v1/categories", new
        {
            nombre = "Longboard",
            descripcion = "Categoria longboard",
            gender = "Ambos",
            ageRestriction = false,
            minAge = (int?)null,
            maxAge = (int?)null,
            successorCategoryId = (string?)null,
            status = "Activo"
        });

        var category = await ReadJsonAsync(categoryResponse);
        var categoryId = category.RootElement.GetProperty("id").GetString();

        // Tarifa de la categoria para 3 estrellas (nivel del evento) y para 5 estrellas (override).
        var tariff3Response = await _client.PutAsJsonAsync($"/v1/categories/{categoryId}/tariffs/3", new
        {
            usd = 75,
            cop = 308000,
            active = true
        });
        Assert.Equal(HttpStatusCode.OK, tariff3Response.StatusCode);

        var tariff5Response = await _client.PutAsJsonAsync($"/v1/categories/{categoryId}/tariffs/5", new
        {
            usd = 120,
            cop = 492000,
            active = true
        });
        Assert.Equal(HttpStatusCode.OK, tariff5Response.StatusCode);

        // useCircuitTariffs=true, con la categoria fijada a 5 estrellas (distinto de las 3 del evento).
        var updateResponse = await _client.PutAsJsonAsync($"/v1/events/{eventId}/categories", new
        {
            useCircuitTariffs = true,
            categories = new[]
            {
                new
                {
                    categoryId,
                    stars = 5,
                    customTariffUsd = (decimal?)null,
                    capacidad = (int?)null
                }
            }
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var getAfterUpdate = await _client.GetAsync($"/v1/events/{eventId}/categories");
        Assert.Equal(HttpStatusCode.OK, getAfterUpdate.StatusCode);

        var updated = await ReadJsonAsync(getAfterUpdate);
        var entry = updated.RootElement.GetProperty("data")[0];

        Assert.Equal(5, entry.GetProperty("stars").GetInt32());
        // Debe resolver la tarifa de 5 estrellas (120), no la de 3 estrellas del evento (75).
        Assert.Equal(120, entry.GetProperty("effectiveTariffUsd").GetDouble());

        // Sin override (stars = null), debe caer de vuelta al nivel del evento (3 estrellas -> 75).
        var clearOverrideResponse = await _client.PutAsJsonAsync($"/v1/events/{eventId}/categories", new
        {
            useCircuitTariffs = true,
            categories = new[]
            {
                new
                {
                    categoryId,
                    stars = (int?)null,
                    customTariffUsd = (decimal?)null,
                    capacidad = (int?)null
                }
            }
        });

        Assert.Equal(HttpStatusCode.OK, clearOverrideResponse.StatusCode);

        var getAfterClear = await _client.GetAsync($"/v1/events/{eventId}/categories");
        var cleared = await ReadJsonAsync(getAfterClear);
        var clearedEntry = cleared.RootElement.GetProperty("data")[0];

        Assert.False(clearedEntry.TryGetProperty("stars", out var starsProperty));
        Assert.Equal(JsonValueKind.Undefined, starsProperty.ValueKind);
        Assert.Equal(75, clearedEntry.GetProperty("effectiveTariffUsd").GetDouble());

        var clearAssignmentsResponse = await _client.PutAsJsonAsync($"/v1/events/{eventId}/categories", new
        {
            useCircuitTariffs = true,
            categories = Array.Empty<object>()
        });
        Assert.Equal(HttpStatusCode.OK, clearAssignmentsResponse.StatusCode);

        var deleteCategoryResponse = await _client.DeleteAsync($"/v1/categories/{categoryId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteCategoryResponse.StatusCode);
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(content);
    }
}
