using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace AlasApp.Api.Tests;

public sealed class CircuitsAndEventsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CircuitsAndEventsEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CircuitCrudFlow_Works_EndToEnd()
    {
        var createResponse = await _client.PostAsJsonAsync("/v1/circuits", new
        {
            nombre = "ALAS Open",
            temporada = 2026,
            descripcion = "Circuito principal",
            region = "Latinoamérica",
            modalidad = "Shortboard",
            estado = "Activo",
            surfScoresCode = "ALAS-OPEN-26"
        });

        var createBody = await createResponse.Content.ReadAsStringAsync();
        if (createResponse.StatusCode != HttpStatusCode.Created)
        {
            throw new InvalidOperationException(
                $"Expected 201 Created but received {(int)createResponse.StatusCode} {createResponse.StatusCode}. Body: {createBody}");
        }
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = JsonDocument.Parse(createBody);
        var circuitId = created.RootElement.GetProperty("id").GetString();
        Assert.Equal("ALAS Open", created.RootElement.GetProperty("nombre").GetString());
        Assert.False(string.IsNullOrWhiteSpace(circuitId));

        var getResponse = await _client.GetAsync($"/v1/circuits/{circuitId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var listResponse = await _client.GetAsync("/v1/circuits?page=1&limit=20&year=2026");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var updateResponse = await _client.PutAsJsonAsync($"/v1/circuits/{circuitId}", new
        {
            nombre = "ALAS Open Updated",
            temporada = 2026,
            descripcion = "Circuito actualizado",
            region = "Latinoamérica",
            modalidad = "Longboard",
            estado = "Borrador",
            surfScoresCode = "ALAS-OPEN-26-UPD"
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updated = await ReadJsonAsync(updateResponse);
        Assert.Equal("ALAS Open Updated", updated.RootElement.GetProperty("nombre").GetString());

        var deleteResponse = await _client.DeleteAsync($"/v1/circuits/{circuitId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task EventCrudFlow_Works_EndToEnd_AndPreventsDeletingCircuitWithEvents()
    {
        var circuitResponse = await _client.PostAsJsonAsync("/v1/circuits", new
        {
            nombre = "ALAS Surf",
            temporada = 2026,
            descripcion = "Circuito para eventos",
            region = "Latinoamérica",
            modalidad = "Shortboard",
            estado = "Activo",
            surfScoresCode = "ALAS-SURF-26"
        });

        var circuitBody = await circuitResponse.Content.ReadAsStringAsync();
        if (circuitResponse.StatusCode != HttpStatusCode.Created)
        {
            throw new InvalidOperationException(
                $"Expected 201 Created but received {(int)circuitResponse.StatusCode} {circuitResponse.StatusCode}. Body: {circuitBody}");
        }

        var circuit = JsonDocument.Parse(circuitBody);
        var circuitId = circuit.RootElement.GetProperty("id").GetString();
        Assert.False(string.IsNullOrWhiteSpace(circuitId));

        var createEventResponse = await _client.PostAsJsonAsync("/v1/events", new
        {
            nombre = "Roca Bruja Classic",
            circuitId,
            fechaInicio = "2026-07-12",
            fechaFin = "2026-07-15",
            pais = "Perú",
            ciudad = "Chicama",
            playa = "Roca Bruja",
            stars = 3,
            capacidadMaxima = 60,
            prizeAmountUsd = 5000,
            surfScoresCode = "RBC-2026",
            accessType = "Abierto",
            estado = "Activo"
        });

        Assert.Equal(HttpStatusCode.Created, createEventResponse.StatusCode);

        var createdEvent = await ReadJsonAsync(createEventResponse);
        var eventId = createdEvent.RootElement.GetProperty("id").GetString();
        Assert.Equal("Roca Bruja Classic", createdEvent.RootElement.GetProperty("nombre").GetString());

        var listEventsResponse = await _client.GetAsync($"/v1/events?circuitId={circuitId}&status=Inscripciones%20Abiertas");
        var listEventsBody = await listEventsResponse.Content.ReadAsStringAsync();
        if (listEventsResponse.StatusCode != HttpStatusCode.OK)
        {
            throw new InvalidOperationException(
                $"Expected 200 OK but received {(int)listEventsResponse.StatusCode} {listEventsResponse.StatusCode}. Body: {listEventsBody}");
        }
        Assert.Equal(HttpStatusCode.OK, listEventsResponse.StatusCode);

        var updateEventResponse = await _client.PutAsJsonAsync($"/v1/events/{eventId}", new
        {
            nombre = "Roca Bruja Updated",
            circuitId,
            fechaInicio = "2026-07-12",
            fechaFin = "2026-07-16",
            pais = "Perú",
            ciudad = "Chicama",
            playa = "Roca Bruja",
            stars = 4,
            capacidadMaxima = 80,
            prizeAmountUsd = 7500,
            surfScoresCode = "RBC-2026-UPD",
            accessType = "Restringido",
            estado = "Borrador"
        });

        var updateEventBody = await updateEventResponse.Content.ReadAsStringAsync();
        if (updateEventResponse.StatusCode != HttpStatusCode.OK)
        {
            throw new InvalidOperationException(
                $"Expected 200 OK but received {(int)updateEventResponse.StatusCode} {updateEventResponse.StatusCode}. Body: {updateEventBody}");
        }

        Assert.Equal(HttpStatusCode.OK, updateEventResponse.StatusCode);

        var deleteCircuitResponse = await _client.DeleteAsync($"/v1/circuits/{circuitId}");
        Assert.Equal(HttpStatusCode.Conflict, deleteCircuitResponse.StatusCode);

        var deleteEventResponse = await _client.DeleteAsync($"/v1/events/{eventId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteEventResponse.StatusCode);
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(content);
    }
}
