using AlasApp.Domain.Enums;

namespace AlasApp.Application.Categories.Models;

public sealed record CategoryDto(
    Guid Id,
    string Nombre,
    string? Descripcion,
    CategoryGender Gender,
    bool AgeRestriction,
    int? MinAge,
    int? MaxAge,
    Guid? SuccessorCategoryId,
    CategorySummaryDto? SuccessorCategory,
    CategoryStatus Status,
    decimal MembresiaAnualUsd,
    decimal MembresiaPorEventoUsd,
    int BestResultsCount,
    DateTimeOffset CreatedAtUtc,
    string? SurfScoresCode);
