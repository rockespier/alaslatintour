namespace AlasApp.Application.SurfScoresImport.Models;

public sealed record SurfScoresImportResultDto(
    int TotalFetched,
    IReadOnlyCollection<SurfScoresImportedEventDto> Created,
    IReadOnlyCollection<SurfScoresImportSkippedEventDto> Skipped);

public sealed record SurfScoresImportedEventDto(
    Guid EventId,
    string Nombre,
    string SurfScoresCode,
    int CategoriesLinked,
    IReadOnlyCollection<string> UnmatchedCategoryCodes);

public sealed record SurfScoresImportSkippedEventDto(
    string Nombre,
    string SurfScoresCode,
    string Reason);
