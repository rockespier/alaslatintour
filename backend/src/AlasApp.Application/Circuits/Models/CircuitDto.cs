using AlasApp.Domain.Enums;

namespace AlasApp.Application.Circuits.Models;

public sealed record CircuitDto(
    Guid Id,
    string Nombre,
    int Temporada,
    string? Descripcion,
    CircuitRegion Region,
    CircuitModalidad Modalidad,
    CircuitStatus Estado,
    string? SurfScoresCode,
    int EventsCount,
    int CompetidoresCount,
    decimal TotalPrizeUsd,
    DateTimeOffset? LastSyncAt,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
