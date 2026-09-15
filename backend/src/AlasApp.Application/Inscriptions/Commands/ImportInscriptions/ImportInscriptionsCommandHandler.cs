using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.AdminSettings;
using AlasApp.Application.BulkImports.Models;
using AlasApp.Application.Common;
using AlasApp.Domain.Entities;
using AlasApp.Domain.Enums;
using AlasApp.Domain.Exceptions;

namespace AlasApp.Application.Inscriptions.Commands.ImportInscriptions;

public sealed class ImportInscriptionsCommandHandler(
    IBulkExcelService bulkExcelService,
    ICompetitorRepository competitorRepository,
    IInscriptionRepository inscriptionRepository,
    IAdminSettingsRepository adminSettingsRepository,
    IUnitOfWork unitOfWork,
    IClock clock,
    IImportErrorLogWriter errorLogWriter)
    : IRequestHandler<ImportInscriptionsCommand, BulkImportResultDto>
{
    public async Task<BulkImportResultDto> Handle(ImportInscriptionsCommand request, CancellationToken cancellationToken)
    {
        var pricingContext = await inscriptionRepository.GetPricingContextAsync(request.EventId, request.CategoryId, cancellationToken)
            ?? throw new NotFoundException("Evento o categoria no encontrados para la inscripcion.");

        var settingsJson = await adminSettingsRepository.GetJsonAsync(AdminSettingsDefaults.SettingsKey, cancellationToken);
        var settings = AdminSettingsSerializer.DeserializeOrDefault(settingsJson);
        var administrativeFeeUsd = settings.General.AdministrativeFeeUsd;

        var rows = bulkExcelService.ReadInscriptions(request.FileContent).ToList();
        var errors = new List<BulkImportErrorDto>();
        var created = 0;
        var enrolled = await inscriptionRepository.CountByEventCategoryAsync(request.EventId, request.CategoryId, cancellationToken);

        foreach (var row in rows)
        {
            try
            {
                var competitor = await ResolveCompetitorAsync(row, cancellationToken)
                    ?? throw new InvalidOperationException($"Fila {row.RowNumber}: no se encontro un competidor con los datos indicados (CompetidorId, SurfScoresCode o Email).");

                if (competitor.LicenseStatus != LicenseStatus.Activa)
                {
                    throw new InvalidOperationException($"Fila {row.RowNumber}: el competidor {competitor.Nombre} {competitor.Apellido} no tiene licencia activa.");
                }

                if (!IsGenderCompatible(competitor.Genero, pricingContext.CategoryGender))
                {
                    throw new InvalidOperationException($"Fila {row.RowNumber}: la categoria seleccionada no corresponde al genero del competidor.");
                }

                if (await inscriptionRepository.ExistsDuplicateAsync(competitor.Id, request.EventId, request.CategoryId, cancellationToken))
                {
                    throw new InvalidOperationException($"Fila {row.RowNumber}: el competidor ya esta inscrito en esta categoria del evento.");
                }

                if (pricingContext.CategoryCapacity.HasValue && enrolled >= pricingContext.CategoryCapacity.Value)
                {
                    throw new InvalidOperationException($"Fila {row.RowNumber}: el cupo de la categoria para este evento esta agotado.");
                }

                var paymentMethod = ParseEnum<PaymentMethod>(row.RowNumber, "MetodoPago", row.MetodoPago);
                var estadoAdmin = ParseEnum<InscriptionStatusAdmin>(row.RowNumber, "EstadoAdmin", row.EstadoAdmin);
                var membershipPlan = ParseOptionalEnum<MembershipPlanOption>(row.RowNumber, "MembershipPlan", row.MembershipPlan);

                var montoUsd = pricingContext.UseCircuitTariffs
                    ? pricingContext.CircuitTariffUsd ?? 0m
                    : pricingContext.CustomTariffUsd ?? pricingContext.CircuitTariffUsd ?? 0m;

                var membershipFeeUsd = membershipPlan switch
                {
                    MembershipPlanOption.Anual => pricingContext.MembresiaAnualUsd,
                    MembershipPlanOption.PorEvento => pricingContext.MembresiaPorEventoUsd,
                    _ => 0m
                };

                var totalMontoUsd = montoUsd + administrativeFeeUsd + membershipFeeUsd;

                var inscription = Inscription.Create(
                    competitor.Id,
                    request.EventId,
                    request.CategoryId,
                    row.NumeroCamiseta,
                    paymentMethod,
                    montoUsd,
                    administrativeFeeUsd,
                    membershipPlan,
                    membershipFeeUsd,
                    totalMontoUsd,
                    true,
                    true,
                    true,
                    clock.UtcNow);

                inscription.SetCreated(clock.UtcNow);
                inscription.Update(row.NumeroCamiseta, estadoAdmin, row.Notas);

                var transaccionId = row.TransaccionId?.Trim();
                if (!string.IsNullOrWhiteSpace(transaccionId))
                {
                    inscription.ApplyPayment(paymentMethod, transaccionId, estadoAdmin);
                }

                await inscriptionRepository.AddAsync(inscription, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                enrolled++;
                created++;
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

        var errorLogFile = errorLogWriter.Write("inscripciones", errors);

        return new BulkImportResultDto(rows.Count, created, 0, errors, errorLogFile);
    }

    private async Task<Competitor?> ResolveCompetitorAsync(InscriptionImportRow row, CancellationToken cancellationToken)
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

    private static bool IsGenderCompatible(CompetitorGender competitorGender, CategoryGender categoryGender)
    {
        return categoryGender == CategoryGender.Ambos
            || (categoryGender == CategoryGender.Masculino && competitorGender == CompetitorGender.Masculino)
            || (categoryGender == CategoryGender.Femenino && competitorGender == CompetitorGender.Femenino);
    }

    private static TEnum ParseEnum<TEnum>(int rowNumber, string field, string? value) where TEnum : struct
    {
        if (string.IsNullOrWhiteSpace(value) || !Enum.TryParse<TEnum>(value.Trim(), true, out var parsed))
        {
            throw new InvalidOperationException($"Fila {rowNumber}: el campo '{field}' es obligatorio y debe ser un valor valido.");
        }

        return parsed;
    }

    private static TEnum? ParseOptionalEnum<TEnum>(int rowNumber, string field, string? value) where TEnum : struct
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!Enum.TryParse<TEnum>(value.Trim(), true, out var parsed))
        {
            throw new InvalidOperationException($"Fila {rowNumber}: el valor '{value}' no es valido para '{field}'.");
        }

        return parsed;
    }
}
