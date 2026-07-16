using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.BulkImports.Models;
using AlasApp.Domain.Entities;
using AlasApp.Domain.Enums;

namespace AlasApp.Application.Events.Commands.ImportEvents;

public sealed class ImportEventsCommandHandler(
    IBulkExcelService bulkExcelService,
    IEventRepository eventRepository,
    ICircuitRepository circuitRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ImportEventsCommand, BulkImportResultDto>
{
    public async Task<BulkImportResultDto> Handle(ImportEventsCommand request, CancellationToken cancellationToken)
    {
        var rows = bulkExcelService.ReadEvents(request.FileContent);
        var errors = new List<BulkImportErrorDto>();
        var created = 0;
        var updated = 0;

        foreach (var row in rows)
        {
            try
            {
                var entity = await ResolveEntityAsync(row, cancellationToken);
                var circuitId = await ResolveCircuitIdAsync(row, cancellationToken);
                var nombre = Required(row.RowNumber, "Nombre", row.Nombre);
                var fechaInicio = ParseDate(row.RowNumber, "FechaInicio", row.FechaInicio);
                var fechaFin = ParseDate(row.RowNumber, "FechaFin", row.FechaFin);
                var pais = Required(row.RowNumber, "Pais", row.Pais);
                var ciudad = Required(row.RowNumber, "Ciudad", row.Ciudad);
                var playa = Required(row.RowNumber, "Playa", row.Playa);
                var stars = ParseInt(row.RowNumber, "Stars", row.Stars);
                var capacidadMaxima = ParseInt(row.RowNumber, "CapacidadMaxima", row.CapacidadMaxima);
                var prizeAmountUsd = ParseDecimal(row.RowNumber, "PrizeAmountUsd", row.PrizeAmountUsd);
                var eventType = ParseEnum<EventType>(row.RowNumber, "EventType", row.EventType);
                var accessType = ParseEnum<EventAccessType>(row.RowNumber, "AccessType", row.AccessType);
                var estado = ParseEnum<EventStatusAdmin>(row.RowNumber, "Estado", row.Estado);

                if (entity is null)
                {
                    entity = Event.Create(
                        circuitId,
                        nombre,
                        fechaInicio,
                        fechaFin,
                        pais,
                        ciudad,
                        playa,
                        row.Auspiciador,
                        stars,
                        capacidadMaxima,
                        prizeAmountUsd,
                        row.ImagenUrl,
                        row.SurfScoresCode,
                        eventType,
                        accessType,
                        estado);

                    await eventRepository.AddAsync(entity, cancellationToken);
                    created++;
                }
                else
                {
                    entity.Update(
                        circuitId,
                        nombre,
                        fechaInicio,
                        fechaFin,
                        pais,
                        ciudad,
                        playa,
                        row.Auspiciador,
                        stars,
                        capacidadMaxima,
                        prizeAmountUsd,
                        row.ImagenUrl,
                        row.SurfScoresCode,
                        eventType,
                        accessType,
                        estado);

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

    private async Task<Event?> ResolveEntityAsync(EventImportRow row, CancellationToken cancellationToken)
    {
        if (Guid.TryParse(row.Id, out var eventId))
        {
            return await eventRepository.GetEntityByIdAsync(eventId, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(row.SurfScoresCode))
        {
            return await eventRepository.GetEntityBySurfScoresCodeAsync(row.SurfScoresCode.Trim(), cancellationToken);
        }

        return null;
    }

    private async Task<Guid> ResolveCircuitIdAsync(EventImportRow row, CancellationToken cancellationToken)
    {
        if (Guid.TryParse(row.CircuitId, out var circuitId))
        {
            var circuit = await circuitRepository.GetEntityByIdAsync(circuitId, cancellationToken);
            if (circuit is not null)
            {
                return circuit.Id;
            }
        }

        if (!string.IsNullOrWhiteSpace(row.CircuitSurfScoresCode))
        {
            var circuit = await circuitRepository.GetEntityBySurfScoresCodeAsync(row.CircuitSurfScoresCode.Trim(), cancellationToken);
            if (circuit is not null)
            {
                return circuit.Id;
            }
        }

        throw new InvalidOperationException($"Fila {row.RowNumber}: no se encontró el circuito referenciado.");
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

    private static decimal ParseDecimal(int rowNumber, string field, string? value)
    {
        if (!decimal.TryParse(Required(rowNumber, field, value), out var parsed))
        {
            throw new InvalidOperationException($"Fila {rowNumber}: el campo '{field}' debe ser decimal.");
        }

        return parsed;
    }

    private static DateTimeOffset ParseDate(int rowNumber, string field, string? value)
    {
        if (!DateTimeOffset.TryParse(Required(rowNumber, field, value), out var parsed))
        {
            throw new InvalidOperationException($"Fila {rowNumber}: el campo '{field}' debe ser una fecha válida.");
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
