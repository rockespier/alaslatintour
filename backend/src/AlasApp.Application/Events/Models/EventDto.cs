using AlasApp.Domain.Enums;

namespace AlasApp.Application.Events.Models;

public sealed record EventDto(
    Guid Id,
    Guid CircuitId,
    string Nombre,
    DateTimeOffset FechaInicio,
    DateTimeOffset FechaFin,
    string Pais,
    string Ciudad,
    string Playa,
    int Stars,
    int CapacidadMaxima,
    decimal PrizeAmountUsd,
    string? SurfScoresCode,
    EventAccessType AccessType,
    EventStatusAdmin Estado,
    int EnrolledCount,
    EventStatusPublic StatusPublic,
    string Lugar,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
