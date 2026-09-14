using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.BulkImports.Models;

namespace AlasApp.Infrastructure.Imports;

public sealed class FileImportErrorLogWriter : IImportErrorLogWriter
{
    private static readonly string LogsDirectory = Path.Combine(AppContext.BaseDirectory, "logs");

    public string? Write(string importType, IReadOnlyCollection<BulkImportErrorDto> errors)
    {
        if (errors.Count == 0)
        {
            return null;
        }

        Directory.CreateDirectory(LogsDirectory);

        var fileName = $"import-{importType}-errores-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.txt";
        var filePath = Path.Combine(LogsDirectory, fileName);

        var lines = errors
            .OrderBy(error => error.RowNumber)
            .Select(error => $"Fila {error.RowNumber}: {error.Message}");

        File.WriteAllLines(filePath, lines);

        return filePath;
    }
}
