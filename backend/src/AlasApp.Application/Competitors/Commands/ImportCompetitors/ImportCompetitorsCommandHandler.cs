using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.BulkImports.Models;
using AlasApp.Domain.Entities;
using AlasApp.Domain.Enums;
using AlasApp.Domain.Exceptions;

namespace AlasApp.Application.Competitors.Commands.ImportCompetitors;

public sealed class ImportCompetitorsCommandHandler(
    IBulkExcelService bulkExcelService,
    ICompetitorRepository competitorRepository,
    IUnitOfWork unitOfWork,
    IClock clock)
    : IRequestHandler<ImportCompetitorsCommand, BulkImportResultDto>
{
    public async Task<BulkImportResultDto> Handle(ImportCompetitorsCommand request, CancellationToken cancellationToken)
    {
        var rows = bulkExcelService.ReadCompetitors(request.FileContent).ToList();
        var errors = new List<BulkImportErrorDto>();
        var created = 0;
        var updated = 0;

        foreach (var row in rows)
        {
            try
            {
                var email = Required(row.RowNumber, "Email", row.Email);
                var entity = await ResolveEntityAsync(row, email, cancellationToken);

                var nombre = Required(row.RowNumber, "Nombre", row.Nombre);
                var apellido = Required(row.RowNumber, "Apellido", row.Apellido);
                var fechaNacimiento = ParseDate(row.RowNumber, "FechaNacimiento", row.FechaNacimiento);
                var genero = ParseEnum<CompetitorGender>(row.RowNumber, "Genero", row.Genero);
                var pais = Required(row.RowNumber, "Pais", row.Pais);
                var postura = ParseEnum<CompetitorPostura>(row.RowNumber, "Postura", row.Postura);
                var tallaCamiseta = ParseEnum<CompetitorShirtSize>(row.RowNumber, "TallaCamiseta", row.TallaCamiseta);

                if (entity is null)
                {
                    if (await competitorRepository.EmailExistsAsync(email, null, cancellationToken))
                    {
                        throw new InvalidOperationException($"Fila {row.RowNumber}: ya existe un competidor con el email '{email}'.");
                    }

                    entity = Competitor.Create(
                        nombre,
                        apellido,
                        email,
                        fechaNacimiento,
                        genero,
                        pais,
                        row.Telefono ?? string.Empty,
                        row.Club ?? string.Empty,
                        postura,
                        tallaCamiseta,
                        row.NumeroCamiseta ?? string.Empty,
                        row.Patrocinadores ?? string.Empty,
                        row.Federacion ?? string.Empty,
                        row.SurfScoresCode);

                    entity.SetCreated(clock.UtcNow);
                    await competitorRepository.AddAsync(entity, cancellationToken);
                    created++;
                }
                else
                {
                    if (await competitorRepository.EmailExistsAsync(email, entity.Id, cancellationToken))
                    {
                        throw new InvalidOperationException($"Fila {row.RowNumber}: ya existe otro competidor con el email '{email}'.");
                    }

                    entity.Update(
                        nombre,
                        apellido,
                        email,
                        fechaNacimiento,
                        genero,
                        pais,
                        row.Telefono ?? string.Empty,
                        row.Club ?? string.Empty,
                        postura,
                        tallaCamiseta,
                        row.NumeroCamiseta ?? string.Empty,
                        row.Patrocinadores ?? string.Empty,
                        row.Federacion ?? string.Empty,
                        row.SurfScoresCode);

                    entity.SetUpdated(clock.UtcNow);
                    updated++;
                }

                await unitOfWork.SaveChangesAsync(cancellationToken);
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

        return new BulkImportResultDto(rows.Count, created, updated, errors);
    }

    private async Task<Competitor?> ResolveEntityAsync(CompetitorImportRow row, string email, CancellationToken cancellationToken)
    {
        if (Guid.TryParse(row.Id, out var competitorId))
        {
            return await competitorRepository.GetEntityByIdAsync(competitorId, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(row.SurfScoresCode))
        {
            var byCode = await competitorRepository.GetEntityBySurfScoresCodeAsync(row.SurfScoresCode.Trim(), cancellationToken);
            if (byCode is not null)
            {
                return byCode;
            }
        }

        return await competitorRepository.GetEntityByEmailAsync(email, cancellationToken);
    }

    private static string Required(int rowNumber, string field, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Fila {rowNumber}: el campo '{field}' es obligatorio.");
        }

        return value.Trim();
    }

    private static DateTimeOffset ParseDate(int rowNumber, string field, string? value)
    {
        if (!DateTimeOffset.TryParse(Required(rowNumber, field, value), out var parsed))
        {
            throw new InvalidOperationException($"Fila {rowNumber}: el campo '{field}' debe ser una fecha valida.");
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
