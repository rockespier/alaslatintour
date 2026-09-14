namespace AlasApp.Api.Models;

public sealed record BulkImportResponse(
    int ProcessedRows,
    int CreatedCount,
    int UpdatedCount,
    IReadOnlyCollection<BulkImportErrorResponse> Errors,
    string? ErrorLogFile = null);

public sealed record BulkImportErrorResponse(
    int RowNumber,
    string Message);
