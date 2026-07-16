using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.BulkImports.Models;
using AlasApp.Domain.Entities;
using AlasApp.Domain.Enums;

namespace AlasApp.Application.Categories.Commands.ImportCategories;

public sealed class ImportCategoriesCommandHandler(
    IBulkExcelService bulkExcelService,
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ImportCategoriesCommand, BulkImportResultDto>
{
    public async Task<BulkImportResultDto> Handle(ImportCategoriesCommand request, CancellationToken cancellationToken)
    {
        var rows = bulkExcelService.ReadCategories(request.FileContent).ToList();
        var errors = new List<BulkImportErrorDto>();
        var created = 0;
        var updated = 0;
        var staged = new List<(CategoryImportRow Row, Category Entity, bool IsNew)>();

        foreach (var row in rows)
        {
            try
            {
                var entity = await ResolveEntityAsync(row, cancellationToken);
                var nombre = Required(row.RowNumber, "Nombre", row.Nombre);
                var gender = ParseEnum<CategoryGender>(row.RowNumber, "Gender", row.Gender);
                var ageRestriction = ParseBool(row.RowNumber, "AgeRestriction", row.AgeRestriction);
                var minAge = ParseNullableInt(row.RowNumber, "MinAge", row.MinAge);
                var maxAge = ParseNullableInt(row.RowNumber, "MaxAge", row.MaxAge);
                var status = ParseEnum<CategoryStatus>(row.RowNumber, "Status", row.Status);
                var membresiaAnualUsd = ParseDecimal(row.RowNumber, "MembresiaAnualUsd", row.MembresiaAnualUsd);
                var membresiaPorEventoUsd = ParseDecimal(row.RowNumber, "MembresiaPorEventoUsd", row.MembresiaPorEventoUsd);
                var bestResultsCount = ParseInt(row.RowNumber, "BestResultsCount", row.BestResultsCount);

                if (entity is null)
                {
                    entity = Category.Create(
                        nombre,
                        row.Descripcion,
                        gender,
                        ageRestriction,
                        minAge,
                        maxAge,
                        null,
                        status,
                        membresiaAnualUsd,
                        membresiaPorEventoUsd,
                        bestResultsCount,
                        row.SurfScoresCode);

                    await categoryRepository.AddAsync(entity, cancellationToken);
                    created++;
                    staged.Add((row, entity, true));
                }
                else
                {
                    entity.Update(
                        nombre,
                        row.Descripcion,
                        gender,
                        ageRestriction,
                        minAge,
                        maxAge,
                        null,
                        status,
                        membresiaAnualUsd,
                        membresiaPorEventoUsd,
                        bestResultsCount,
                        row.SurfScoresCode);

                    updated++;
                    staged.Add((row, entity, false));
                }
            }
            catch (Exception ex)
            {
                errors.Add(new BulkImportErrorDto(row.RowNumber, ex.Message));
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var item in staged)
        {
            try
            {
                var successorId = await ResolveSuccessorIdAsync(item.Row, staged, cancellationToken);
                item.Entity.Update(
                    item.Entity.Nombre,
                    item.Entity.Descripcion,
                    item.Entity.Gender,
                    item.Entity.AgeRestriction,
                    item.Entity.MinAge,
                    item.Entity.MaxAge,
                    successorId,
                    item.Entity.Status,
                    item.Entity.MembresiaAnualUsd,
                    item.Entity.MembresiaPorEventoUsd,
                    item.Entity.BestResultsCount,
                    item.Entity.SurfScoresCode);

                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                errors.Add(new BulkImportErrorDto(item.Row.RowNumber, ex.Message));
            }
        }

        return new BulkImportResultDto(rows.Count, created, updated, errors);
    }

    private async Task<Category?> ResolveEntityAsync(CategoryImportRow row, CancellationToken cancellationToken)
    {
        if (Guid.TryParse(row.Id, out var categoryId))
        {
            return await categoryRepository.GetEntityByIdAsync(categoryId, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(row.SurfScoresCode))
        {
            var byCode = await categoryRepository.GetEntityBySurfScoresCodeAsync(row.SurfScoresCode.Trim(), cancellationToken);
            if (byCode is not null)
            {
                return byCode;
            }
        }

        if (!string.IsNullOrWhiteSpace(row.Nombre))
        {
            return await categoryRepository.GetEntityByNameAsync(row.Nombre.Trim(), cancellationToken);
        }

        return null;
    }

    private async Task<Guid?> ResolveSuccessorIdAsync(
        CategoryImportRow row,
        IReadOnlyCollection<(CategoryImportRow Row, Category Entity, bool IsNew)> staged,
        CancellationToken cancellationToken)
    {
        if (Guid.TryParse(row.SuccessorCategoryId, out var successorId))
        {
            var entity = await categoryRepository.GetEntityByIdAsync(successorId, cancellationToken);
            if (entity is null)
            {
                throw new InvalidOperationException($"Fila {row.RowNumber}: no se encontró la categoría sucesora indicada.");
            }

            return entity.Id;
        }

        if (!string.IsNullOrWhiteSpace(row.SuccessorSurfScoresCode))
        {
            var code = row.SuccessorSurfScoresCode.Trim();
            var stagedEntity = staged.FirstOrDefault(x => string.Equals(x.Entity.SurfScoresCode, code, StringComparison.OrdinalIgnoreCase)).Entity;
            if (stagedEntity is not null)
            {
                return stagedEntity.Id;
            }

            var persisted = await categoryRepository.GetEntityBySurfScoresCodeAsync(code, cancellationToken);
            if (persisted is not null)
            {
                return persisted.Id;
            }

            throw new InvalidOperationException($"Fila {row.RowNumber}: no se encontró la categoría sucesora con SurfScoresCode '{code}'.");
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

    private static int? ParseNullableInt(int rowNumber, string field, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!int.TryParse(value.Trim(), out var parsed))
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

    private static bool ParseBool(int rowNumber, string field, string? value)
    {
        if (!bool.TryParse(Required(rowNumber, field, value), out var parsed))
        {
            throw new InvalidOperationException($"Fila {rowNumber}: el campo '{field}' debe ser true o false.");
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
