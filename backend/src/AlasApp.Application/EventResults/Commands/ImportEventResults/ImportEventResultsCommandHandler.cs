using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.BulkImports.Models;
using AlasApp.Application.EventResults.Models;

namespace AlasApp.Application.EventResults.Commands.ImportEventResults;

public sealed class ImportEventResultsCommandHandler(
    IBulkExcelService bulkExcelService,
    EventResultsWriter writer)
    : IRequestHandler<ImportEventResultsCommand, BulkImportResultDto>
{
    public async Task<BulkImportResultDto> Handle(ImportEventResultsCommand request, CancellationToken cancellationToken)
    {
        var rows = bulkExcelService.ReadEventResults(request.FileContent);
        var errors = new List<BulkImportErrorDto>();
        var items = new List<EventResultUpsertItem>();
        var seenCompetitors = new HashSet<Guid>();

        foreach (var row in rows)
        {
            var place = row.Puesto?.Trim();
            if (string.IsNullOrWhiteSpace(place))
            {
                continue;
            }

            try
            {
                var competitorId = ParseGuid(row.RowNumber, "CompetidorId", row.CompetitorId);
                if (!seenCompetitors.Add(competitorId))
                {
                    throw new InvalidOperationException($"Fila {row.RowNumber}: el competidor ya aparece en otra fila del archivo.");
                }

                items.Add(new EventResultUpsertItem(
                    competitorId,
                    place,
                    ParseOptionalInt(row.RowNumber, "PuntosLiga", row.PuntosLiga) ?? 0,
                    ParseOptionalDecimal(row.RowNumber, "PremioUsd", row.PremioUsd),
                    ParseOptionalDecimal(row.RowNumber, "HeatOla1", row.HeatOla1),
                    ParseOptionalDecimal(row.RowNumber, "HeatOla2", row.HeatOla2)));
            }
            catch (Exception ex)
            {
                errors.Add(new BulkImportErrorDto(row.RowNumber, ex.Message));
            }
        }

        if (items.Count == 0)
        {
            return new BulkImportResultDto(rows.Count, 0, 0, errors);
        }

        var outcome = await writer.UpsertAsync(request.EventId, request.CategoryId, items, cancellationToken);
        return new BulkImportResultDto(rows.Count, outcome.CreatedCount, outcome.UpdatedCount, errors);
    }

    private static Guid ParseGuid(int rowNumber, string field, string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !Guid.TryParse(value.Trim(), out var parsed))
        {
            throw new InvalidOperationException($"Fila {rowNumber}: el campo '{field}' es obligatorio y debe ser un identificador valido. No modifiques la columna '{field}' de la plantilla.");
        }

        return parsed;
    }

    private static int? ParseOptionalInt(int rowNumber, string field, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!int.TryParse(value.Trim(), out var parsed))
        {
            throw new InvalidOperationException($"Fila {rowNumber}: el campo '{field}' debe ser numerico.");
        }

        return parsed;
    }

    private static decimal? ParseOptionalDecimal(int rowNumber, string field, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!decimal.TryParse(value.Trim(), out var parsed))
        {
            throw new InvalidOperationException($"Fila {rowNumber}: el campo '{field}' debe ser decimal.");
        }

        return parsed;
    }
}
