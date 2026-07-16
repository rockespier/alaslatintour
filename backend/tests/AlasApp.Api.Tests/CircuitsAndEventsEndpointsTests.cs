using ClosedXML.Excel;
using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using Xunit;

namespace AlasApp.Api.Tests;

public sealed class CircuitsAndEventsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CircuitsAndEventsEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CircuitCrudFlow_Works_EndToEnd()
    {
        await TestAdminAuthHelper.AuthenticateAsAdminAsync(_client, _factory.Services);

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
        await TestAdminAuthHelper.AuthenticateAsAdminAsync(_client, _factory.Services);

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
            imagenUrl = "https://cdn.test/events/roca-bruja-poster.jpg",
            pais = "Perú",
            ciudad = "Chicama",
            playa = "Roca Bruja",
            auspiciador = "Monster Energy",
            stars = 6,
            capacidadMaxima = 60,
            prizeAmountUsd = 5000,
            surfScoresCode = "RBC-2026",
            eventType = "Prime",
            accessType = "Abierto",
            estado = "Activo"
        });

        Assert.Equal(HttpStatusCode.Created, createEventResponse.StatusCode);

        var createdEvent = await ReadJsonAsync(createEventResponse);
        var eventId = createdEvent.RootElement.GetProperty("id").GetString();
        Assert.Equal("Roca Bruja Classic", createdEvent.RootElement.GetProperty("nombre").GetString());
        Assert.Equal("https://cdn.test/events/roca-bruja-poster.jpg", createdEvent.RootElement.GetProperty("imagenUrl").GetString());
        Assert.Equal("Monster Energy", createdEvent.RootElement.GetProperty("auspiciador").GetString());
        Assert.Equal("Prime", createdEvent.RootElement.GetProperty("eventType").GetString());
        Assert.Equal(6, createdEvent.RootElement.GetProperty("stars").GetInt32());

        var getEventResponse = await _client.GetAsync($"/v1/events/{eventId}");
        Assert.Equal(HttpStatusCode.OK, getEventResponse.StatusCode);
        using var getEventJson = await ReadJsonAsync(getEventResponse);
        Assert.Equal("https://cdn.test/events/roca-bruja-poster.jpg", getEventJson.RootElement.GetProperty("imagenUrl").GetString());
        Assert.Equal("Monster Energy", getEventJson.RootElement.GetProperty("auspiciador").GetString());
        Assert.Equal("Prime", getEventJson.RootElement.GetProperty("eventType").GetString());

        var listEventsResponse = await _client.GetAsync($"/v1/events?circuitId={circuitId}&status=Inscripciones%20Abiertas");
        var listEventsBody = await listEventsResponse.Content.ReadAsStringAsync();
        if (listEventsResponse.StatusCode != HttpStatusCode.OK)
        {
            throw new InvalidOperationException(
                $"Expected 200 OK but received {(int)listEventsResponse.StatusCode} {listEventsResponse.StatusCode}. Body: {listEventsBody}");
        }
        Assert.Equal(HttpStatusCode.OK, listEventsResponse.StatusCode);
        using var listEventsJson = JsonDocument.Parse(listEventsBody);
        Assert.Equal("https://cdn.test/events/roca-bruja-poster.jpg", listEventsJson.RootElement.GetProperty("data")[0].GetProperty("imagenUrl").GetString());
        Assert.Equal("Monster Energy", listEventsJson.RootElement.GetProperty("data")[0].GetProperty("auspiciador").GetString());
        Assert.Equal("Prime", listEventsJson.RootElement.GetProperty("data")[0].GetProperty("eventType").GetString());

        var updateEventResponse = await _client.PutAsJsonAsync($"/v1/events/{eventId}", new
        {
            nombre = "Roca Bruja Updated",
            circuitId,
            fechaInicio = "2026-07-12",
            fechaFin = "2026-07-16",
            imagenUrl = "https://cdn.test/events/roca-bruja-updated.jpg",
            pais = "Perú",
            ciudad = "Chicama",
            playa = "Roca Bruja",
            auspiciador = "Red Bull",
            stars = 7,
            capacidadMaxima = 80,
            prizeAmountUsd = 7500,
            surfScoresCode = "RBC-2026-UPD",
            eventType = "SuperPrime",
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
        using var updatedEvent = JsonDocument.Parse(updateEventBody);
        Assert.Equal("https://cdn.test/events/roca-bruja-updated.jpg", updatedEvent.RootElement.GetProperty("imagenUrl").GetString());
        Assert.Equal("Red Bull", updatedEvent.RootElement.GetProperty("auspiciador").GetString());
        Assert.Equal("SuperPrime", updatedEvent.RootElement.GetProperty("eventType").GetString());
        Assert.Equal(7, updatedEvent.RootElement.GetProperty("stars").GetInt32());

        var deleteCircuitResponse = await _client.DeleteAsync($"/v1/circuits/{circuitId}");
        Assert.Equal(HttpStatusCode.Conflict, deleteCircuitResponse.StatusCode);

        var deleteEventResponse = await _client.DeleteAsync($"/v1/events/{eventId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteEventResponse.StatusCode);
    }

    [Fact]
    public async Task CircuitsTemplate_AndImportXlsx_Work()
    {
        await TestAdminAuthHelper.AuthenticateAsAdminAsync(_client, _factory.Services);

        var templateResponse = await _client.GetAsync("/v1/circuits/template");
        Assert.Equal(HttpStatusCode.OK, templateResponse.StatusCode);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", templateResponse.Content.Headers.ContentType?.MediaType);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Circuits");
        WriteRow(worksheet, 1, "Id", "SurfScoresCode", "Nombre", "Temporada", "Descripcion", "Region", "Modalidad", "Estado");
        WriteRow(worksheet, 2, "", "CIR-IMPORT-2026", "Circuito Importado", "2026", "Carga masiva", "Latinoamerica", "Shortboard", "Activo");

        var response = await _client.PostAsync("/v1/circuits/import", CreateExcelForm(workbook, "circuits-import.xlsx"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var payload = await ReadJsonAsync(response);
        Assert.Equal(1, payload.RootElement.GetProperty("processedRows").GetInt32());
        Assert.Equal(1, payload.RootElement.GetProperty("createdCount").GetInt32());
        Assert.Equal(0, payload.RootElement.GetProperty("errors").GetArrayLength());

        var listResponse = await _client.GetAsync("/v1/circuits?year=2026");
        using var listJson = await ReadJsonAsync(listResponse);
        Assert.Contains(listJson.RootElement.GetProperty("data").EnumerateArray(), x => x.GetProperty("surfScoresCode").GetString() == "CIR-IMPORT-2026");
    }

    [Fact]
    public async Task EventsImportXlsx_Works_WithCircuitSurfScoresCode()
    {
        await TestAdminAuthHelper.AuthenticateAsAdminAsync(_client, _factory.Services);

        var circuitResponse = await _client.PostAsJsonAsync("/v1/circuits", new
        {
            nombre = "Circuito Import Events",
            temporada = 2026,
            descripcion = "Circuito base",
            region = "Latinoamérica",
            modalidad = "Shortboard",
            estado = "Activo",
            surfScoresCode = "BASE-EVENTS-2026"
        });

        Assert.Equal(HttpStatusCode.Created, circuitResponse.StatusCode);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Events");
        WriteRow(worksheet, 1, "Id", "SurfScoresCode", "CircuitId", "CircuitSurfScoresCode", "Nombre", "FechaInicio", "FechaFin", "Pais", "Ciudad", "Playa", "Auspiciador", "ImagenUrl", "Stars", "CapacidadMaxima", "PrizeAmountUsd", "EventType", "AccessType", "Estado");
        WriteRow(worksheet, 2, "", "EV-IMPORT-2026", "", "BASE-EVENTS-2026", "Evento XLSX", "2026-10-01", "2026-10-03", "Peru", "Lima", "Punta Rocas", "Marca XLSX", "https://cdn.test/xlsx-event.png", "7", "150", "9000.50", "SuperPrime", "Abierto", "Activo");

        var response = await _client.PostAsync("/v1/events/import", CreateExcelForm(workbook, "events-import.xlsx"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var payload = await ReadJsonAsync(response);
        Assert.Equal(1, payload.RootElement.GetProperty("createdCount").GetInt32());
        Assert.Equal(0, payload.RootElement.GetProperty("errors").GetArrayLength());

        var listResponse = await _client.GetAsync("/v1/events?year=2026");
        using var listJson = await ReadJsonAsync(listResponse);
        Assert.Contains(listJson.RootElement.GetProperty("data").EnumerateArray(), x => x.GetProperty("surfScoresCode").GetString() == "EV-IMPORT-2026");
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(content);
    }

    private static MultipartFormDataContent CreateExcelForm(XLWorkbook workbook, string fileName)
    {
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(stream.ToArray());
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        form.Add(fileContent, "file", fileName);
        return form;
    }

    private static void WriteRow(IXLWorksheet worksheet, int rowNumber, params string[] values)
    {
        for (var index = 0; index < values.Length; index++)
        {
            worksheet.Cell(rowNumber, index + 1).Value = values[index];
        }
    }
}
