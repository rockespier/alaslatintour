using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Categories.Models;
using AlasApp.Domain.Enums;

namespace AlasApp.Application.Categories.Commands.UpdateCategory;

public sealed record UpdateCategoryCommand(
    Guid CategoryId,
    string Nombre,
    string? Descripcion,
    CategoryGender Gender,
    bool AgeRestriction,
    int? MinAge,
    int? MaxAge,
    Guid? SuccessorCategoryId,
    CategoryStatus Status,
    decimal MembresiaAnualUsd,
    decimal MembresiaPorEventoUsd,
    int BestResultsCount,
    string? SurfScoresCode) : IRequest<CategoryDto>;
