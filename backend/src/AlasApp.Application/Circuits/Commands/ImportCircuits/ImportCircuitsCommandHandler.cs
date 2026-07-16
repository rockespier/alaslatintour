using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.BulkImports.Models;
using AlasApp.Domain.Entities;
using AlasApp.Domain.Enums;

namespace AlasApp.Application.Circuits.Commands.ImportCircuits;

public sealed class ImportCircuitsCommandHandler(
    IBulkExcelService bulkExcelService,
    ICircuitRepository circuitRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ImportCircuitsCommand, BulkImportResultDto>
{
    public async Task<BulkImportResultDto> Handle(ImportCircuitsCommand request, CancellationToken cancellationToken)
    {
        var rows = bulkExcelService.ReadCircuits(request.FileContent);
        var errors = new List<BulkImportErrorDto>();
        var created = 0;
        var updated = 0;

        foreach (var row in rows)
        {
            try
            {
                var entity = await ResolveEntityAsync(row, cancellationToken);
                var nombre = Required(row.RowNumber, "Nombre", row.Nombre);
                var temporada = ParseInt(row.RowNumber, "Temporada", row.Temporada);
                var region = ParseEnum<CircuitRegion>(row.RowNumber, "Region", row.Region);
                var modalidad = ParseEnum<CircuitModalidad>(row.RowNumber, "Modalidad", row.Modalidad);
                var estado = ParseEnum<CircuitStatus>(row.RowNumber, "Estado", row.Estado);

                if (entity is null)
                {
                    entity = Circuit.Create(nombre, temporada, row.Descripcion, region, modalidad, estado, row.SurfScoresCode);
                    await circuitRepository.AddAsync(entity, cancellationToken);
                    created++;
                }
                else
                {
                    entity.Update(nombre, temporada, row.Descripcion, region, modalidad, estado, row.SurfScoresCode);
                    updated++;
                }

                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                errors.Add(new BulkImportErrorDto(row.RowNumber, ex.Message));
            }
        }

        return new BulkImportResultDto(rows.Count, created, updated, errors);
    }

    private async Task<Circuit?> ResolveEntityAsync(CircuitImportRow row, CancellationToken cancellationToken)
    {
        if (Guid.TryParse(row.Id, out var circuitId))
        {
            return await circuitRepository.GetEntityByIdAsync(circuitId, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(row.SurfScoresCode))
        {
            var byCode = await circuitRepository.GetEntityBySurfScoresCodeAsync(row.SurfScoresCode.Trim(), cancellationToken);
            if (byCode is not null)
            {
                return byCode;
            }
        }

        if (!string.IsNullOrWhiteSpace(row.Nombre) && int.TryParse(row.Temporada, out var temporada))
        {
            return await circuitRepository.GetEntityByNameAndSeasonAsync(row.Nombre.Trim(), temporada, cancellationToken);
        }

        return null;
    }

    private static string Required(int rowNumber, string field, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Fila {rowNumber}: el campo '{field}' es obligatorio.");
        }

        return value.Trim();
    }

    private static int ParseInt(int rowNumber, string field, string? value)
    {
        if (!int.TryParse(Required(rowNumber, field, value), out var parsed))
        {
            throw new InvalidOperationException($"Fila {rowNumber}: el campo '{field}' debe ser numérico.");
        }

        return parsed;
    }

    private static TEnum ParseEnum<TEnum>(int rowNumber, string field, string? value) where TEnum : struct
    {
        if (!Enum.TryParse<TEnum>(Required(rowNumber, field, value), true, out var parsed))
        {
            throw new InvalidOperationException($"Fila {rowNumber}: el valor '{value}' no es válido para '{field}'.");
        }

        return parsed;
    }
}
