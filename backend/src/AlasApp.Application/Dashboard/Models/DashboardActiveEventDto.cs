using AlasApp.Domain.Enums;

namespace AlasApp.Application.Dashboard.Models;

public sealed record DashboardActiveEventDto(
    Guid Id,
    string Nombre,
    DateOnly FechaInicio,
    EventStatusAdmin Estado,
    int InscritosCount);
