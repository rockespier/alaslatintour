using AlasApp.Application.BulkImports.Models;

namespace AlasApp.Application.Abstractions.Services;

public interface IImportErrorLogWriter
{
    string? Write(string importType, IReadOnlyCollection<BulkImportErrorDto> errors);
}
