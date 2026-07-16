namespace AlasApp.Application.BulkImports.Models;

public sealed record BulkImportResultDto(
    int ProcessedRows,
    int CreatedCount,
    int UpdatedCount,
    IReadOnlyCollection<BulkImportErrorDto> Errors);

public sealed record BulkImportErrorDto(
    int RowNumber,
    string Message);

public sealed record CircuitImportRow(
    int RowNumber,
    string? Id,
    string? SurfScoresCode,
    string? Nombre,
    string? Temporada,
    string? Descripcion,
    string? Region,
    string? Modalidad,
    string? Estado);

public sealed record EventImportRow(
    int RowNumber,
    string? Id,
    string? SurfScoresCode,
    string? CircuitId,
    string? CircuitSurfScoresCode,
    string? Nombre,
    string? FechaInicio,
    string? FechaFin,
    string? Pais,
    string? Ciudad,
    string? Playa,
    string? Auspiciador,
    string? ImagenUrl,
    string? Stars,
    string? CapacidadMaxima,
    string? PrizeAmountUsd,
    string? EventType,
    string? AccessType,
    string? Estado);

public sealed record CategoryImportRow(
    int RowNumber,
    string? Id,
    string? SurfScoresCode,
    string? Nombre,
    string? Descripcion,
    string? Gender,
    string? AgeRestriction,
    string? MinAge,
    string? MaxAge,
    string? SuccessorCategoryId,
    string? SuccessorSurfScoresCode,
    string? Status,
    string? MembresiaAnualUsd,
    string? MembresiaPorEventoUsd,
    string? BestResultsCount);
