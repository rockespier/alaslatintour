namespace AlasApp.Application.Competitors.Models;

public sealed record CompetitorPointsHistoryDto(
    Guid CompetitorId,
    int Temporada,
    string CategoryId,
    IReadOnlyCollection<CompetitorPointsHistoryEntryDto> Data,
    CompetitorPointsHistoryStatsDto Stats,
    string Attribution);
