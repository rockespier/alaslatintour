using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.BulkImports.Models;
using AlasApp.Application.Common;
using AlasApp.Domain.Entities;
using AlasApp.Domain.Enums;
using AlasApp.Domain.Exceptions;

namespace AlasApp.Application.Payments.Commands.ImportMembershipPayments;

/// <summary>
/// Adjunta membresia (plan + monto pagado) a inscripciones ya existentes de un evento, y crea el
/// Payment correspondiente si la inscripcion todavia no tiene uno. Pensado para cargar masivamente
/// datos historicos de membresia (ej. inscripciones importadas antes por SQL directo, sin membresia).
/// No crea inscripciones nuevas: si el competidor no tiene inscripcion en el evento, la fila falla.
/// </summary>
public sealed class ImportMembershipPaymentsCommandHandler(
    IBulkExcelService bulkExcelService,
    ICompetitorRepository competitorRepository,
    IEventRepository eventRepository,
    IInscriptionRepository inscriptionRepository,
    IPaymentRepository paymentRepository,
    IUnitOfWork unitOfWork,
    IClock clock,
    IImportErrorLogWriter errorLogWriter)
    : IRequestHandler<ImportMembershipPaymentsCommand, BulkImportResultDto>
{
    public async Task<BulkImportResultDto> Handle(ImportMembershipPaymentsCommand request, CancellationToken cancellationToken)
    {
        _ = await eventRepository.GetEntityByIdAsync(request.EventId, cancellationToken)
            ?? throw new NotFoundException("Evento no encontrado.");

        var rows = bulkExcelService.ReadMembershipPayments(request.FileContent).ToList();
        var errors = new List<BulkImportErrorDto>();
        var updated = 0;

        foreach (var row in rows)
        {
            try
            {
                var competitor = await ResolveCompetitorAsync(row, cancellationToken)
                    ?? throw new InvalidOperationException($"Fila {row.RowNumber}: no se encontro un competidor con los datos indicados (CompetidorId, SurfScoresCode o Email).");

                var candidates = await inscriptionRepository.ListEntitiesByCompetitorAndEventAsync(competitor.Id, request.EventId, cancellationToken);

                Inscription inscription;
                if (!string.IsNullOrWhiteSpace(row.InscripcionId))
                {
                    var inscripcionId = ParseGuid(row.RowNumber, "InscripcionId", row.InscripcionId);
                    inscription = candidates.FirstOrDefault(x => x.Id == inscripcionId)
                        ?? throw new InvalidOperationException($"Fila {row.RowNumber}: la inscripcion indicada no pertenece a este competidor en este evento.");
                }
                else if (candidates.Count == 1)
                {
                    inscription = candidates.Single();
                }
                else if (candidates.Count == 0)
                {
                    throw new InvalidOperationException($"Fila {row.RowNumber}: {competitor.Nombre} {competitor.Apellido} no tiene una inscripcion en este evento. Este import solo agrega membresia a inscripciones existentes.");
                }
                else
                {
                    throw new InvalidOperationException($"Fila {row.RowNumber}: {competitor.Nombre} {competitor.Apellido} tiene mas de una inscripcion en este evento; completa la columna InscripcionId para indicar cual.");
                }

                var plan = ParseEnum<MembershipPlanOption>(row.RowNumber, "TipoMembresia", row.TipoMembresia);
                var fechaPago = ParseDate(row.RowNumber, "FechaPago", row.FechaPago);
                var metodoPago = ParseEnum<PaymentMethod>(row.RowNumber, "MetodoPago", row.MetodoPago);
                var importe = ParseDecimal(row.RowNumber, "Importe", row.Importe);

                inscription.ApplyMembership(plan, importe);

                var existingPayment = await paymentRepository.GetEntityByInscriptionIdAsync(inscription.Id, cancellationToken);
                if (existingPayment is null)
                {
                    var transaccionId = string.IsNullOrWhiteSpace(row.TransaccionId)
                        ? $"BULK-MEMB-{inscription.Id:N}"[..24]
                        : row.TransaccionId!.Trim();

                    var payment = Payment.Create(inscription.Id, metodoPago, inscription.MontoUsd, transaccionId, PaymentStatusAdmin.Confirmado, fechaPago);
                    payment.SetCreated(clock.UtcNow);
                    await paymentRepository.AddAsync(payment, cancellationToken);
                    inscription.ApplyPayment(metodoPago, transaccionId, InscriptionStatusAdmin.Pagado);
                }

                inscription.SetUpdated(clock.UtcNow);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                updated++;
            }
            catch (DomainRuleException exception)
            {
                errors.Add(new BulkImportErrorDto(row.RowNumber, exception.Message));
            }
            catch (Exception ex)
            {
                errors.Add(new BulkImportErrorDto(row.RowNumber, ex.Message));
            }
        }

        var errorLogFile = errorLogWriter.Write("membresias", errors);

        return new BulkImportResultDto(rows.Count, 0, updated, errors, errorLogFile);
    }

    private async Task<Competitor?> ResolveCompetitorAsync(MembershipPaymentImportRow row, CancellationToken cancellationToken)
    {
        if (Guid.TryParse(row.CompetidorId, out var competitorId))
        {
            var byId = await competitorRepository.GetEntityByIdAsync(competitorId, cancellationToken);
            if (byId is not null)
            {
                return byId;
            }
        }

        if (!string.IsNullOrWhiteSpace(row.SurfScoresCode))
        {
            var byCode = await competitorRepository.GetEntityBySurfScoresCodeAsync(row.SurfScoresCode.Trim(), cancellationToken);
            if (byCode is not null)
            {
                return byCode;
            }
        }

        if (!string.IsNullOrWhiteSpace(row.Email))
        {
            return await competitorRepository.GetEntityByEmailAsync(row.Email.Trim(), cancellationToken);
        }

        return null;
    }

    private static Guid ParseGuid(int rowNumber, string field, string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !Guid.TryParse(value.Trim(), out var parsed))
        {
            throw new InvalidOperationException($"Fila {rowNumber}: el campo '{field}' debe ser un identificador valido.");
        }

        return parsed;
    }

    private static TEnum ParseEnum<TEnum>(int rowNumber, string field, string? value) where TEnum : struct
    {
        if (string.IsNullOrWhiteSpace(value) || !Enum.TryParse<TEnum>(value.Trim(), true, out var parsed))
        {
            throw new InvalidOperationException($"Fila {rowNumber}: el campo '{field}' es obligatorio y debe ser un valor valido.");
        }

        return parsed;
    }

    private static DateTimeOffset ParseDate(int rowNumber, string field, string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !DateTimeOffset.TryParse(value.Trim(), out var parsed))
        {
            throw new InvalidOperationException($"Fila {rowNumber}: el campo '{field}' es obligatorio y debe ser una fecha valida (AAAA-MM-DD).");
        }

        return parsed;
    }

    private static decimal ParseDecimal(int rowNumber, string field, string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !decimal.TryParse(value.Trim(), out var parsed) || parsed < 0)
        {
            throw new InvalidOperationException($"Fila {rowNumber}: el campo '{field}' es obligatorio y debe ser un numero valido mayor o igual a 0.");
        }

        return parsed;
    }
}
