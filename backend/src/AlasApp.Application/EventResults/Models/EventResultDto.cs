namespace AlasApp.Application.EventResults.Models;

public sealed record EventResultDto(
    Guid Id,
    Guid EventId,
    Guid CategoryId,
    Guid CompetitorId,
    string CompetitorName,
    string Country,
    string Place,
    int LigaPoints,
    decimal? PrizeUsd,
    decimal? HeatOla1,
    decimal? HeatOla2,
    decimal? HeatScoreTotal);

public sealed record EventResultUpsertItem(
    Guid CompetitorId,
    string Place,
    int LigaPoints,
    decimal? PrizeUsd,
    decimal? HeatOla1,
    decimal? HeatOla2);

public sealed record PrizeDistributionDto(
    int Stars,
    IReadOnlyCollection<PrizeDistributionRowDto> Rows);

public sealed record PrizeDistributionRowDto(
    string PlaceLabel,
    decimal PrizeUsd);

public sealed record EventResultRosterRowDto(
    Guid CompetitorId,
    string CompetitorName,
    string Country,
    string? Place,
    int? LigaPoints,
    decimal? PrizeUsd,
    decimal? HeatOla1,
    decimal? HeatOla2);
