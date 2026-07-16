namespace AlasApp.Api.Models;

public sealed record BulkImportResponse(
    int ProcessedRows,
    int CreatedCount,
    int UpdatedCount,
    IReadOnlyCollection<BulkImportErrorResponse> Errors);

public sealed record BulkImportErrorResponse(
    int RowNumber,
    string Message);
