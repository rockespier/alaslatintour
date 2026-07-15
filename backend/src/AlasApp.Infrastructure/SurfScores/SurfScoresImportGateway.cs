using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.AdminSettings.Models;
using AlasApp.Application.Common;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AlasApp.Infrastructure.SurfScores;

public sealed class SurfScoresImportGateway(HttpClient httpClient, SurfScoresTokenCache tokenCache) : ISurfScoresImportGateway
{
    private const int DefaultTokenTtlSeconds = 1800;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyCollection<SurfScoresRemoteEventDto>> GetOrganizationEventsAsync(
        SurfScoresSettingsDto settings,
        CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync(settings, cancellationToken);

        var payload = await PostAsync<RemoteEventsWireResponse>(
            settings.Endpoint,
            "events/organization",
            new { token, id = settings.OrganizacionId },
            cancellationToken);

        var events = payload?.Events ?? payload?.Data ?? [];

        return events
            .Select(x => new SurfScoresRemoteEventDto(
                ReadId(x.Id),
                x.Name,
                ParseDate(x.StartDate),
                ParseDate(x.EndDate),
                x.Country,
                x.Place))
            .Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .ToList();
    }

    public async Task<IReadOnlyCollection<SurfScoresRemoteCategoryDto>> GetEventCategoriesAsync(
        SurfScoresSettingsDto settings,
        string eventSurfScoresCode,
        CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync(settings, cancellationToken);

        var payload = await PostAsync<RemoteCategoriesWireResponse>(
            settings.Endpoint,
            "events/categories",
            new { token, id = eventSurfScoresCode },
            cancellationToken);

        var categories = payload?.Categories ?? payload?.Data ?? [];

        return categories
            .Select(x => new SurfScoresRemoteCategoryDto(ReadId(x.Id), x.Name))
            .Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .ToList();
    }

    private Task<string> GetTokenAsync(SurfScoresSettingsDto settings, CancellationToken cancellationToken)
    {
        return tokenCache.GetOrRefreshAsync(async ct =>
        {
            var payload = await PostAsync<LoginWireResponse>(
                settings.Endpoint,
                "users/login",
                new
                {
                    email = settings.Username,
                    password = settings.Password,
                    terms_accepted = settings.TermsAccepted
                },
                ct);

            var token = payload?.Token ?? payload?.AccessToken ?? payload?.Data?.Token;
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ValidationException(
                    "SurfScores no devolvio un token de sesion valido al iniciar sesion.",
                    [new ValidationError("surfScores", "La respuesta de login no contiene un token.")]);
            }

            return (token, payload?.ExpiresIn ?? DefaultTokenTtlSeconds);
        }, cancellationToken);
    }

    private async Task<T?> PostAsync<T>(string endpoint, string path, object body, CancellationToken cancellationToken)
    {
        var uri = new Uri(new Uri(endpoint.TrimEnd('/') + "/"), path);

        using var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = JsonContent.Create(body, options: JsonOptions)
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new ValidationException(
                $"La API de SurfScores respondio con un error ({(int)response.StatusCode}) al consultar {path}.",
                [new ValidationError("surfScores", string.IsNullOrWhiteSpace(detail) ? response.ReasonPhrase ?? "Error desconocido." : detail)]);
        }

        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
    }

    private static string ReadId(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.String => element.GetString() ?? string.Empty,
            _ => string.Empty
        };
    }

    private static DateTimeOffset? ParseDate(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && DateTimeOffset.TryParse(value, out var parsed) ? parsed : null;
    }

    private sealed record LoginWireResponse(
        [property: JsonPropertyName("token")] string? Token,
        [property: JsonPropertyName("access_token")] string? AccessToken,
        [property: JsonPropertyName("expires_in")] int? ExpiresIn,
        [property: JsonPropertyName("data")] LoginWireData? Data);

    private sealed record LoginWireData([property: JsonPropertyName("token")] string? Token);

    private sealed record RemoteEventWire(
        [property: JsonPropertyName("id")] JsonElement Id,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("start_date")] string? StartDate,
        [property: JsonPropertyName("end_date")] string? EndDate,
        [property: JsonPropertyName("country")] string? Country,
        [property: JsonPropertyName("place")] string? Place);

    private sealed record RemoteEventsWireResponse(
        [property: JsonPropertyName("events")] List<RemoteEventWire>? Events,
        [property: JsonPropertyName("data")] List<RemoteEventWire>? Data);

    private sealed record RemoteCategoryWire(
        [property: JsonPropertyName("id")] JsonElement Id,
        [property: JsonPropertyName("name")] string? Name);

    private sealed record RemoteCategoriesWireResponse(
        [property: JsonPropertyName("categories")] List<RemoteCategoryWire>? Categories,
        [property: JsonPropertyName("data")] List<RemoteCategoryWire>? Data);
}
