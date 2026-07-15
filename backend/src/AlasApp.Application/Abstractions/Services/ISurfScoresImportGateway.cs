using AlasApp.Application.AdminSettings.Models;

namespace AlasApp.Application.Abstractions.Services;

public interface ISurfScoresImportGateway
{
    Task<IReadOnlyCollection<SurfScoresRemoteEventDto>> GetOrganizationEventsAsync(
        SurfScoresSettingsDto settings,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<SurfScoresRemoteCategoryDto>> GetEventCategoriesAsync(
        SurfScoresSettingsDto settings,
        string eventSurfScoresCode,
        CancellationToken cancellationToken);
}

public sealed record SurfScoresRemoteEventDto(
    string Id,
    string? Name,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate,
    string? Country,
    string? Place);

public sealed record SurfScoresRemoteCategoryDto(string Id, string? Name);
