namespace AlasApp.Application.Rankings.Models;

public sealed record RankingDto(
    Guid CategoryId,
    string CategoryName,
    int Year,
    DateTimeOffset CachedAtUtc,
    IReadOnlyCollection<RankingEntryDto> Entries,
    RankingPaginationDto Pagination);

public sealed record RankingEntryDto(
    int Pos,
    string Name,
    string Country,
    int Points,
    int Events,
    int? Variation);

public sealed record RankingPaginationDto(
    int CurrentPage,
    int ItemsPerPage,
    int TotalItems,
    int TotalPages);

public sealed record RankingCategoryAvailabilityDto(
    Guid CategoryId,
    string CategoryName,
    IReadOnlyCollection<int> AvailableYears);

public sealed record SurfScoresSyncResultDto(
    string CircuitCode,
    int RecordsUpdated,
    DateTimeOffset SyncedAtUtc);

public sealed record SurfScoresRankingSnapshotDto(
    Guid CategoryId,
    string CategoryName,
    int Year,
    IReadOnlyCollection<RankingEntryDto> Entries);
